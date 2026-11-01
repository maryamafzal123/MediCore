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
    public class PrescriptionController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PrescriptionController(AppDbContext context)
        {
            _context = context;
        }

        private static IQueryable<PrescriptionResponseDto> Project(IQueryable<Prescription> q) =>
            q.OrderByDescending(p => p.CreatedAt)
             .Select(p => new PrescriptionResponseDto
             {
                 Id = p.Id,
                 AppointmentId = p.AppointmentId,
                 Diagnosis = p.Diagnosis,
                 Medications = p.Medications,
                 Instructions = p.Instructions,
                 ValidUntil = p.ValidUntil,
                 PatientName = p.Patient.User.FirstName + " " + p.Patient.User.LastName,
                 DoctorName = p.Appointment.Doctor.User.FirstName + " " + p.Appointment.Doctor.User.LastName,
                 AppointmentDate = p.Appointment.AppointmentDate
             });

        // Only the patient, the prescribing doctor, or an admin may see a prescription
        private IQueryable<Prescription> VisibleToCurrentUser()
        {
            var userId = User.GetUserId();
            var q = _context.Prescriptions.AsQueryable();
            if (User.IsInRole("Admin")) return q;
            if (User.IsInRole("Doctor")) return q.Where(p => p.Appointment.Doctor.UserId == userId);
            return q.Where(p => p.Patient.UserId == userId);
        }

        // POST api/prescription
        // Doctor creates a prescription for one of THEIR appointments
        [HttpPost]
        [Authorize(Roles = "Doctor")]
        public async Task<IActionResult> CreatePrescription([FromBody] CreatePrescriptionDto dto)
        {
            var userId = User.GetUserId();
            var appointment = await _context.Appointments
                .Include(a => a.Doctor)
                .FirstOrDefaultAsync(a => a.Id == dto.AppointmentId);

            if (appointment == null)
                return NotFound("Appointment not found");

            if (appointment.Doctor.UserId != userId)
                return Forbid();

            if (appointment.Status == AppointmentStatus.Cancelled)
                return BadRequest("Cannot write a prescription for a cancelled appointment");

            if (await _context.Prescriptions.AnyAsync(p => p.AppointmentId == dto.AppointmentId))
                return BadRequest("Prescription already exists for this appointment");

            var prescription = new Prescription
            {
                AppointmentId = dto.AppointmentId,
                PatientId = appointment.PatientId,   // trust the appointment, not the client
                Diagnosis = dto.Diagnosis,
                Medications = dto.Medications,
                Instructions = dto.Instructions,
                ValidUntil = DateTime.SpecifyKind(dto.ValidUntil.ToUniversalTime(), DateTimeKind.Utc),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Writing a prescription completes the appointment
            appointment.Status = AppointmentStatus.Completed;
            appointment.UpdatedAt = DateTime.UtcNow;

            _context.Prescriptions.Add(prescription);
            await _context.SaveChangesAsync();

            return Ok("Prescription created successfully");
        }

        // GET api/prescription/myprescriptions
        // Patient → own prescriptions, Doctor → prescriptions they wrote, Admin → all
        [HttpGet("myprescriptions")]
        [Authorize]
        public async Task<IActionResult> GetMyPrescriptions()
        {
            return Ok(await Project(VisibleToCurrentUser()).ToListAsync());
        }

        // GET api/prescription/5
        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetPrescription(int id)
        {
            var result = await Project(VisibleToCurrentUser().Where(p => p.Id == id)).FirstOrDefaultAsync();
            return result == null ? NotFound("Prescription not found") : Ok(result);
        }

        // GET api/prescription/appointment/5
        [HttpGet("appointment/{appointmentId}")]
        [Authorize]
        public async Task<IActionResult> GetByAppointment(int appointmentId)
        {
            var result = await Project(VisibleToCurrentUser().Where(p => p.AppointmentId == appointmentId)).FirstOrDefaultAsync();
            return result == null ? NotFound("Prescription not found") : Ok(result);
        }
    }
}
