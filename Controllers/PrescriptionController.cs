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
    public class PrescriptionController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PrescriptionController(AppDbContext context)
        {
            _context = context;
        }

        // POST api/prescription
        // Doctor creates a prescription
        [HttpPost]
        [Authorize(Roles = "Doctor")]
        public async Task<IActionResult> CreatePrescription([FromBody] CreatePrescriptionDto dto)
        {
            // Check appointment exists
            var appointment = await _context.Appointments
                .FirstOrDefaultAsync(a => a.Id == dto.AppointmentId);

            if (appointment == null)
                return NotFound("Appointment not found");

            // Check prescription doesn't already exist
            if (await _context.Prescriptions.AnyAsync(p => p.AppointmentId == dto.AppointmentId))
                return BadRequest("Prescription already exists for this appointment");

            var prescription = new Prescription
            {
                AppointmentId = dto.AppointmentId,
                PatientId = dto.PatientId,
                Diagnosis = dto.Diagnosis,
                Medications = dto.Medications,
                Instructions = dto.Instructions,
                ValidUntil = dto.ValidUntil,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Prescriptions.Add(prescription);
            await _context.SaveChangesAsync();

            return Ok("Prescription created successfully");
        }

        // GET api/prescription/myrescriptions
        // Patient views their own prescriptions
        [HttpGet("myprescriptions")]
        [Authorize(Roles = "Patient")]
        public async Task<IActionResult> GetMyPrescriptions()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var patient = await _context.Patients
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (patient == null)
                return NotFound("Patient profile not found");

            var prescriptions = await _context.Prescriptions
                .Include(p => p.Appointment)
                    .ThenInclude(a => a.Doctor)
                        .ThenInclude(d => d.User)
                .Where(p => p.PatientId == patient.Id)
                .Select(p => new PrescriptionResponseDto
                {
                    Id = p.Id,
                    Diagnosis = p.Diagnosis,
                    Medications = p.Medications,
                    Instructions = p.Instructions,
                    ValidUntil = p.ValidUntil,
                    PatientName = $"{patient.User.FirstName} {patient.User.LastName}",
                    DoctorName = $"{p.Appointment.Doctor.User.FirstName} {p.Appointment.Doctor.User.LastName}",
                    AppointmentDate = p.Appointment.AppointmentDate
                })
                .ToListAsync();

            return Ok(prescriptions);
        }

        // GET api/prescription/5
        // Get prescription by id
        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetPrescription(int id)
        {
            var prescription = await _context.Prescriptions
                .Include(p => p.Patient).ThenInclude(p => p.User)
                .Include(p => p.Appointment)
                    .ThenInclude(a => a.Doctor)
                        .ThenInclude(d => d.User)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (prescription == null)
                return NotFound("Prescription not found");

            return Ok(new PrescriptionResponseDto
            {
                Id = prescription.Id,
                Diagnosis = prescription.Diagnosis,
                Medications = prescription.Medications,
                Instructions = prescription.Instructions,
                ValidUntil = prescription.ValidUntil,
                PatientName = $"{prescription.Patient.User.FirstName} {prescription.Patient.User.LastName}",
                DoctorName = $"{prescription.Appointment.Doctor.User.FirstName} {prescription.Appointment.Doctor.User.LastName}",
                AppointmentDate = prescription.Appointment.AppointmentDate
            });
        }

        // GET api/prescription/appointment/5
        // Get prescription by appointment id
        [HttpGet("appointment/{appointmentId}")]
        [Authorize]
        public async Task<IActionResult> GetByAppointment(int appointmentId)
        {
            var prescription = await _context.Prescriptions
                .Include(p => p.Patient).ThenInclude(p => p.User)
                .Include(p => p.Appointment)
                    .ThenInclude(a => a.Doctor)
                        .ThenInclude(d => d.User)
                .FirstOrDefaultAsync(p => p.AppointmentId == appointmentId);

            if (prescription == null)
                return NotFound("Prescription not found");

            return Ok(new PrescriptionResponseDto
            {
                Id = prescription.Id,
                Diagnosis = prescription.Diagnosis,
                Medications = prescription.Medications,
                Instructions = prescription.Instructions,
                ValidUntil = prescription.ValidUntil,
                PatientName = $"{prescription.Patient.User.FirstName} {prescription.Patient.User.LastName}",
                DoctorName = $"{prescription.Appointment.Doctor.User.FirstName} {prescription.Appointment.Doctor.User.LastName}",
                AppointmentDate = prescription.Appointment.AppointmentDate
            });
        }
    }
}