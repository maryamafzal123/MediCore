using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using MediCore.Data;
using MediCore.DTOs;
using MediCore.Models;
using System.Security.Claims;

namespace MediCore.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AppointmentController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AppointmentController(AppDbContext context)
        {
            _context = context;
        }

        // POST api/appointment
        // Patient books an appointment
        [HttpPost]
        [Authorize(Roles = "Patient")]
        public async Task<IActionResult> BookAppointment([FromBody] CreateAppointmentDto dto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            // Get patient profile from userId
            var patient = await _context.Patients
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (patient == null)
                return BadRequest("Please create patient profile first");

            // Check doctor exists
            var doctor = await _context.Doctors
                .FirstOrDefaultAsync(d => d.Id == dto.DoctorId);

            if (doctor == null)
                return NotFound("Doctor not found");

            var appointment = new Appointment
            {
                DoctorId = dto.DoctorId,
                PatientId = patient.Id,
                AppointmentDate = dto.AppointmentDate,
                Notes = dto.Notes,
                Status = AppointmentStatus.Scheduled,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();

            return Ok("Appointment booked successfully");
        }

        // GET api/appointment
        // Admin gets all appointments
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllAppointments()
        {
            var appointments = await _context.Appointments
                .Include(a => a.Doctor).ThenInclude(d => d.User)
                .Include(a => a.Patient).ThenInclude(p => p.User)
                .Select(a => new AppointmentResponseDto
                {
                    Id = a.Id,
                    AppointmentDate = a.AppointmentDate,
                    Notes = a.Notes,
                    Status = a.Status.ToString(),
                    DoctorName = $"{a.Doctor.User.FirstName} {a.Doctor.User.LastName}",
                    PatientName = $"{a.Patient.User.FirstName} {a.Patient.User.LastName}",
                    Specialization = a.Doctor.Specialization
                })
                .ToListAsync();

            return Ok(appointments);
        }

        // GET api/appointment/myappointments
        // Patient sees their own appointments
        [HttpGet("myappointments")]
        [Authorize(Roles = "Patient")]
        public async Task<IActionResult> GetMyAppointments()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var patient = await _context.Patients
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (patient == null)
                return NotFound("Patient profile not found");

            var appointments = await _context.Appointments
                .Include(a => a.Doctor).ThenInclude(d => d.User)
                .Where(a => a.PatientId == patient.Id)
                .Select(a => new AppointmentResponseDto
                {
                    Id = a.Id,
                    AppointmentDate = a.AppointmentDate,
                    Notes = a.Notes,
                    Status = a.Status.ToString(),
                    DoctorName = $"{a.Doctor.User.FirstName} {a.Doctor.User.LastName}",
                    Specialization = a.Doctor.Specialization
                })
                .ToListAsync();

            return Ok(appointments);
        }

        // GET api/appointment/doctorappontments
        // Doctor sees their own appointments
        [HttpGet("doctorappointments")]
        [Authorize(Roles = "Doctor")]
        public async Task<IActionResult> GetDoctorAppointments()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var doctor = await _context.Doctors
                .FirstOrDefaultAsync(d => d.UserId == userId);

            if (doctor == null)
                return NotFound("Doctor profile not found");

            var appointments = await _context.Appointments
                .Include(a => a.Patient).ThenInclude(p => p.User)
                .Where(a => a.DoctorId == doctor.Id)
                .Select(a => new AppointmentResponseDto
                {
                    Id = a.Id,
                    AppointmentDate = a.AppointmentDate,
                    Notes = a.Notes,
                    Status = a.Status.ToString(),
                    PatientName = $"{a.Patient.User.FirstName} {a.Patient.User.LastName}",
                })
                .ToListAsync();

            return Ok(appointments);
        }

        // PUT api/appointment/5/status
        // Doctor updates appointment status
        [HttpPut("{id}/status")]
        [Authorize(Roles = "Doctor")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateAppointmentStatusDto dto)
        {
            var appointment = await _context.Appointments.FindAsync(id);

            if (appointment == null)
                return NotFound("Appointment not found");

            appointment.Status = dto.Status;
            appointment.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok("Appointment status updated successfully");
        }

        // DELETE api/appointment/5
        // Patient cancels their appointment
        [HttpDelete("{id}")]
        [Authorize(Roles = "Patient,Admin")]
        public async Task<IActionResult> CancelAppointment(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);

            if (appointment == null)
                return NotFound("Appointment not found");

            appointment.Status = AppointmentStatus.Cancelled;
            appointment.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok("Appointment cancelled successfully");
        }
    }
}