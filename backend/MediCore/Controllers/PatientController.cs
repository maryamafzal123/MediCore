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
    public class PatientController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PatientController(AppDbContext context)
        {
            _context = context;
        }

        // POST api/patient/profile
        // Patient creates their profile after registration
        [HttpPost("profile")]
        [Authorize(Roles = "Patient")]
        public async Task<IActionResult> CreateProfile([FromBody] CreatePatientDto dto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            if (await _context.Patients.AnyAsync(p => p.UserId == userId))
                return BadRequest("Patient profile already exists");

            var patient = new Patient
            {
                UserId = userId,
                PhoneNumber = dto.PhoneNumber,
                DateOfBirth = DateTime.SpecifyKind(dto.DateOfBirth.Date, DateTimeKind.Utc),
                Gender = dto.Gender,
                Address = dto.Address,
                BloodGroup = dto.BloodGroup,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Patients.Add(patient);
            await _context.SaveChangesAsync();

            return Ok("Patient profile created successfully");
        }

        // GET api/patient
        // Admin gets all patients
        [HttpGet]
        [Authorize(Roles = "Admin,Doctor")]
        public async Task<IActionResult> GetAllPatients()
        {
            var patients = await _context.Patients
                .Include(p => p.User)
                .Select(p => new PatientResponseDto
                {
                    Id = p.Id,
                    FullName = p.User.FirstName + " " + p.User.LastName,
                    Email = p.User.Email,
                    PhoneNumber = p.PhoneNumber,
                    Gender = p.Gender,
                    BloodGroup = p.BloodGroup,
                    Address = p.Address,
                    DateOfBirth = p.DateOfBirth
                })
                .ToListAsync();

            return Ok(patients);
        }

        // GET api/patient/5
        [HttpGet("{id:int}")]
        [Authorize(Roles = "Admin,Doctor")]
        public async Task<IActionResult> GetPatient(int id)
        {
            var patient = await _context.Patients
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (patient == null)
                return NotFound("Patient not found");

            return Ok(new PatientResponseDto
            {
                Id = patient.Id,
                FullName = $"{patient.User.FirstName} {patient.User.LastName}",
                Email = patient.User.Email,
                PhoneNumber = patient.PhoneNumber,
                Gender = patient.Gender,
                BloodGroup = patient.BloodGroup,
                Address = patient.Address,
                DateOfBirth = patient.DateOfBirth
            });
        }

        // GET api/patient/myprofile
        // Patient views their own profile
        [HttpGet("myprofile")]
        [Authorize(Roles = "Patient")]
        public async Task<IActionResult> GetMyProfile()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var patient = await _context.Patients
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (patient == null)
                return NotFound("Patient profile not found");

            return Ok(new PatientResponseDto
            {
                Id = patient.Id,
                FullName = $"{patient.User.FirstName} {patient.User.LastName}",
                Email = patient.User.Email,
                PhoneNumber = patient.PhoneNumber,
                Gender = patient.Gender,
                BloodGroup = patient.BloodGroup,
                Address = patient.Address,
                DateOfBirth = patient.DateOfBirth
            });
        }

        // PUT api/patient/profile
        [HttpPut("profile")]
        [Authorize(Roles = "Patient")]
        public async Task<IActionResult> UpdateProfile([FromBody] CreatePatientDto dto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var patient = await _context.Patients
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (patient == null)
                return NotFound("Patient profile not found");

            patient.PhoneNumber = dto.PhoneNumber;
            patient.DateOfBirth = DateTime.SpecifyKind(dto.DateOfBirth.Date, DateTimeKind.Utc);
            patient.Gender = dto.Gender;
            patient.Address = dto.Address;
            patient.BloodGroup = dto.BloodGroup;
            patient.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok("Patient profile updated successfully");
        }
    }
}