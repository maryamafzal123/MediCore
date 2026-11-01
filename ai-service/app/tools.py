"""Tools the agent can call. Each tool = one capability of the hospital system.

Tools are built per request (closures over the user's API client),
and each role only gets the tools that make sense for it.
"""
import json
from datetime import datetime

from langchain_core.tools import tool

from .api_client import ApiError, MediCoreApi, local_to_utc_iso, to_local
from .rag import get_kb


def _out(data) -> str:
    return json.dumps(data, ensure_ascii=False, default=str)


def _appointment(a: dict) -> dict:
    return {
        "appointment_id": a["id"],
        "date_time": to_local(a["appointmentDate"]),
        "status": a["status"],
        "doctor": a.get("doctorName"),
        "specialization": a.get("specialization"),
        "patient": a.get("patientName"),
        "patient_id": a.get("patientId"),
        "notes": a.get("notes"),
        "has_prescription": a.get("hasPrescription"),
    }


def _prescription(p: dict) -> dict:
    return {
        "prescription_id": p["id"],
        "appointment_date": to_local(p["appointmentDate"]),
        "doctor": p["doctorName"],
        "patient": p["patientName"],
        "diagnosis": p["diagnosis"],
        "medications": p["medications"],
        "instructions": p["instructions"],
        "valid_until": to_local(p["validUntil"])[:10],
    }


def build_tools(api: MediCoreApi, role: str) -> list:
    # ---------------- Shared tools ----------------

    @tool
    def search_hospital_info(query: str) -> str:
        """Search the hospital handbook: timings, visiting hours, departments, fees and payment,
        cancellation policy, emergency, lab tests, pharmacy, insurance, location, contact, how to use MediCore.
        Use this for any general/policy question instead of guessing."""
        results = get_kb().search(query)
        if not results:
            return "No matching information in the hospital handbook."
        return "\n\n---\n\n".join(f"[{c.source} › {c.title}]\n{c.text}" for c in results)

    @tool
    async def list_doctors(specialization: str = "") -> str:
        """List doctors with id, name, specialization and consultation fee (PKR).
        Optionally filter by specialization, e.g. 'Cardiology', 'Dermatology', 'Pediatrics'.
        Leave empty to list all doctors."""
        try:
            params = {"specialization": specialization} if specialization else None
            doctors = await api.request("GET", "/api/doctor", params=params)
        except ApiError as e:
            return f"Error: {e}"
        if not doctors and specialization:
            return f"No doctor found for '{specialization}'. Call list_doctors with no filter to see all specializations."
        return _out([{"doctor_id": d["id"], "name": f"Dr. {d['fullName']}", "specialization": d["specialization"],
                      "fee_pkr": d["consultationFee"]} for d in doctors])

    @tool
    async def check_doctor_availability(doctor_id: int, date: str) -> str:
        """Get free time slots for a doctor on a date. date format: YYYY-MM-DD (hospital local date).
        Returns the available HH:MM start times."""
        try:
            slots = await api.request("GET", "/api/appointment/availability",
                                      params={"doctorId": doctor_id, "date": date})
        except ApiError as e:
            return f"Error: {e}"
        free = [s["localTime"] for s in slots if s["isAvailable"]]
        if not free:
            return f"No free slots on {date}. Try another date."
        return _out({"doctor_id": doctor_id, "date": date, "available_times": free})

    tools = [search_hospital_info, list_doctors, check_doctor_availability]

    # ---------------- Patient tools ----------------
    if role == "Patient":

        @tool
        async def get_my_appointments(status: str = "") -> str:
            """Get the current patient's appointments. Optional status filter: Scheduled, Completed or Cancelled."""
            try:
                items = await api.request("GET", "/api/appointment/myappointments")
            except ApiError as e:
                return f"Error: {e}"
            if status:
                items = [a for a in items if a["status"].lower() == status.lower()]
            return _out([_appointment(a) for a in items]) if items else "No appointments found."

        @tool
        async def book_appointment(doctor_id: int, date: str, time: str, notes: str = "") -> str:
            """Book an appointment for the current patient.
            date: YYYY-MM-DD, time: HH:MM (24h, hospital local time, must be a free slot).
            notes: short reason for visit.
            ONLY call this after the user has explicitly confirmed doctor, date and time."""
            try:
                a = await api.request("POST", "/api/appointment", json={
                    "doctorId": doctor_id,
                    "appointmentDate": local_to_utc_iso(date, time),
                    "notes": notes[:500],
                })
            except ValueError:
                return "Error: date must be YYYY-MM-DD and time HH:MM."
            except ApiError as e:
                return f"Error: {e}"
            return "Booked successfully: " + _out(_appointment(a))

        @tool
        async def cancel_appointment(appointment_id: int) -> str:
            """Cancel one of the current patient's scheduled appointments.
            ONLY call this after the user has explicitly confirmed which appointment to cancel."""
            try:
                return str(await api.request("DELETE", f"/api/appointment/{appointment_id}"))
            except ApiError as e:
                return f"Error: {e}"

        @tool
        async def get_my_prescriptions() -> str:
            """Get the current patient's prescriptions (diagnosis, medicines, instructions)."""
            try:
                items = await api.request("GET", "/api/prescription/myprescriptions")
            except ApiError as e:
                return f"Error: {e}"
            return _out([_prescription(p) for p in items]) if items else "No prescriptions found."

        @tool
        async def get_my_profile() -> str:
            """Get the current patient's profile (phone, date of birth, gender, blood group, address)."""
            try:
                p = await api.request("GET", "/api/patient/myprofile")
            except ApiError as e:
                return f"Error: {e}"
            p["dateOfBirth"] = str(p.get("dateOfBirth", ""))[:10]
            return _out(p)

        tools += [get_my_appointments, book_appointment, cancel_appointment, get_my_prescriptions, get_my_profile]

    # ---------------- Doctor tools ----------------
    if role == "Doctor":

        @tool
        async def get_my_schedule(date: str = "", status: str = "") -> str:
            """Get the current doctor's appointments.
            Optional date filter YYYY-MM-DD (hospital local) and status filter (Scheduled/Completed/Cancelled)."""
            try:
                items = [_appointment(a) for a in await api.request("GET", "/api/appointment/doctorappointments")]
            except ApiError as e:
                return f"Error: {e}"
            if date:
                items = [a for a in items if a["date_time"].startswith(date)]
            if status:
                items = [a for a in items if a["status"].lower() == status.lower()]
            items.sort(key=lambda a: a["date_time"])
            return _out(items) if items else "No appointments found for that filter."

        @tool
        async def update_appointment_status(appointment_id: int, status: str) -> str:
            """Change the status of one of the doctor's appointments. status: Scheduled, Completed or Cancelled.
            ONLY call after the doctor has confirmed."""
            try:
                return str(await api.request("PUT", f"/api/appointment/{appointment_id}/status",
                                             json={"status": status.capitalize()}))
            except ApiError as e:
                return f"Error: {e}"

        @tool
        async def get_patient_details(patient_id: int) -> str:
            """Get a patient's profile (contact, gender, blood group, date of birth) by patient_id."""
            try:
                p = await api.request("GET", f"/api/patient/{patient_id}")
            except ApiError as e:
                return f"Error: {e}"
            p["dateOfBirth"] = str(p.get("dateOfBirth", ""))[:10]
            return _out(p)

        @tool
        async def get_prescriptions_i_wrote() -> str:
            """Get prescriptions written by the current doctor."""
            try:
                items = await api.request("GET", "/api/prescription/myprescriptions")
            except ApiError as e:
                return f"Error: {e}"
            return _out([_prescription(p) for p in items]) if items else "No prescriptions yet."

        tools += [get_my_schedule, update_appointment_status, get_patient_details, get_prescriptions_i_wrote]

    # ---------------- Admin tools ----------------
    if role == "Admin":

        @tool
        async def get_hospital_stats() -> str:
            """Get hospital-wide statistics: totals of doctors, patients, appointments, prescriptions,
            today's appointments, appointments by status, doctors by specialization, last 7 days trend."""
            try:
                return _out(await api.request("GET", "/api/admin/stats"))
            except ApiError as e:
                return f"Error: {e}"

        @tool
        async def search_appointments(status: str = "", date: str = "", doctor_name: str = "",
                                      patient_name: str = "") -> str:
            """Search all hospital appointments. All filters optional:
            status (Scheduled/Completed/Cancelled), date YYYY-MM-DD, doctor_name, patient_name (partial match)."""
            try:
                items = [_appointment(a) for a in await api.request("GET", "/api/appointment")]
            except ApiError as e:
                return f"Error: {e}"
            if status:
                items = [a for a in items if a["status"].lower() == status.lower()]
            if date:
                items = [a for a in items if a["date_time"].startswith(date)]
            if doctor_name:
                items = [a for a in items if doctor_name.lower() in (a["doctor"] or "").lower()]
            if patient_name:
                items = [a for a in items if patient_name.lower() in (a["patient"] or "").lower()]
            return _out(items[:30]) if items else "No appointments match."

        @tool
        async def list_patients(name: str = "") -> str:
            """List registered patients (optionally filter by partial name)."""
            try:
                items = await api.request("GET", "/api/patient")
            except ApiError as e:
                return f"Error: {e}"
            if name:
                items = [p for p in items if name.lower() in p["fullName"].lower()]
            return _out([{"patient_id": p["id"], "name": p["fullName"], "email": p["email"],
                          "phone": p["phoneNumber"], "gender": p["gender"], "blood_group": p["bloodGroup"]}
                         for p in items[:50]])

        tools += [get_hospital_stats, search_appointments, list_patients]

    return tools
