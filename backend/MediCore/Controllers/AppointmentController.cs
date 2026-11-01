using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using MediCore.Data;
using MediCore.DTOs;
using MediCore.Models;
using MediCore.Services;

namespace MediCore.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AppointmentController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly HospitalSchedule _schedule;

        public AppointmentController(AppDbContext context, HospitalSchedule schedule)
        {
            _context = context;
            _schedule = schedule;
        }

        // Reusable projection Entity → DTO
        private static IQueryable<AppointmentResponseDto> Project(IQueryable<Appointment> q) =>
            q.OrderByDescending(a => a.AppointmentDate)
             .Select(a => new AppointmentResponseDto
             {
                 Id = a.Id,
                 DoctorId = a.DoctorId,
                 PatientId = a.PatientId,
                 HasPrescription = a.Prescription != null,
                 AppointmentDate = a.AppointmentDate,
                 Notes = a.Notes,
                 Status = a.Status.ToString(),
                 DoctorName = a.Doctor.User.FirstName + " " + a.Doctor.User.LastName,
                 PatientName = a.Patient.User.FirstName + " " + a.Patient.User.LastName,
                 Specialization = a.Doctor.Specialization
             });

        // POST api/appointment
        // Patient books an appointment
        [HttpPost]
        [Authorize(Roles = "Patient")]
        public async Task<IActionResult> BookAppointment([FromBody] CreateAppointmentDto dto)
        {
            var userId = User.GetUserId();

            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserId == userId);
            if (patient == null)
                return BadRequest("Please create patient profile first");

            var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.Id == dto.DoctorId);
            if (doctor == null)
                return NotFound("Doctor not found");

            var date = DateTime.SpecifyKind(dto.AppointmentDate.ToUniversalTime(), DateTimeKind.Utc);

            if (!_schedule.IsValidSlot(date))
                return BadRequest($"Appointments are available every {_schedule.SlotMinutes} minutes between " +
                                  $"{_schedule.OpeningHour}:00 and {_schedule.ClosingHour}:00 (hospital time)");

            // Prevent double booking of the same doctor slot
            var taken = await _context.Appointments.AnyAsync(a =>
                a.DoctorId == dto.DoctorId && a.AppointmentDate == date && a.Status != AppointmentStatus.Cancelled);
            if (taken)
                return Conflict("This time slot is already booked. Please choose another slot.");

            var appointment = new Appointment
            {
                DoctorId = dto.DoctorId,
                PatientId = patient.Id,
                AppointmentDate = date,
                Notes = dto.Notes,
                Status = AppointmentStatus.Scheduled,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();

            var result = await Project(_context.Appointments.Where(a => a.Id == appointment.Id)).FirstAsync();
            return Ok(result);
        }

        // GET api/appointment/availability?doctorId=1&date=2026-11-02
        // Anyone logged in can see a doctor's free/booked slots for a date (hospital local date)
        [HttpGet("availability")]
        [Authorize]
        public async Task<IActionResult> GetAvailability([FromQuery] int doctorId, [FromQuery] DateOnly date)
        {
            if (!await _context.Doctors.AnyAsync(d => d.Id == doctorId))
                return NotFound("Doctor not found");

            var slots = _schedule.SlotsFor(date).ToList();
            var from = slots.First();
            var to = slots.Last();

            var booked = await _context.Appointments
                .Where(a => a.DoctorId == doctorId && a.Status != AppointmentStatus.Cancelled
                            && a.AppointmentDate >= from && a.AppointmentDate <= to)
                .Select(a => a.AppointmentDate)
                .ToListAsync();

            var now = DateTime.UtcNow;
            var result = slots.Select(s => new TimeSlotDto
            {
                StartUtc = s,
                LocalTime = _schedule.ToLocal(s).ToString("HH:mm"),
                IsAvailable = s > now && !booked.Contains(s)
            });

            return Ok(result);
        }

        // GET api/appointment
        // Admin gets all appointments
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllAppointments()
        {
            return Ok(await Project(_context.Appointments).ToListAsync());
        }

        // GET api/appointment/myappointments
        // Patient sees their own appointments
        [HttpGet("myappointments")]
        [Authorize(Roles = "Patient")]
        public async Task<IActionResult> GetMyAppointments()
        {
            var userId = User.GetUserId();
            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserId == userId);
            if (patient == null)
                return NotFound("Patient profile not found");

            return Ok(await Project(_context.Appointments.Where(a => a.PatientId == patient.Id)).ToListAsync());
        }

        // GET api/appointment/doctorappointments
        // Doctor sees their own appointments
        [HttpGet("doctorappointments")]
        [Authorize(Roles = "Doctor")]
        public async Task<IActionResult> GetDoctorAppointments()
        {
            var userId = User.GetUserId();
            var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == userId);
            if (doctor == null)
                return NotFound("Doctor profile not found");

            return Ok(await Project(_context.Appointments.Where(a => a.DoctorId == doctor.Id)).ToListAsync());
        }

        // PUT api/appointment/5/status
        // Doctor updates status of THEIR OWN appointment
        [HttpPut("{id}/status")]
        [Authorize(Roles = "Doctor")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateAppointmentStatusDto dto)
        {
            var userId = User.GetUserId();
            var appointment = await _context.Appointments
                .Include(a => a.Doctor)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (appointment == null)
                return NotFound("Appointment not found");

            if (appointment.Doctor.UserId != userId)
                return Forbid();

            appointment.Status = dto.Status;
            appointment.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok("Appointment status updated successfully");
        }

        // DELETE api/appointment/5
        // Patient cancels THEIR OWN appointment (Admin can cancel any)
        [HttpDelete("{id}")]
        [Authorize(Roles = "Patient,Admin")]
        public async Task<IActionResult> CancelAppointment(int id)
        {
            var appointment = await _context.Appointments
                .Include(a => a.Patient)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (appointment == null)
                return NotFound("Appointment not found");

            if (User.IsInRole("Patient") && appointment.Patient.UserId != User.GetUserId())
                return Forbid();

            if (appointment.Status != AppointmentStatus.Scheduled)
                return BadRequest($"Only scheduled appointments can be cancelled (current status: {appointment.Status})");

            appointment.Status = AppointmentStatus.Cancelled;
            appointment.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok("Appointment cancelled successfully");
        }
    }
}
