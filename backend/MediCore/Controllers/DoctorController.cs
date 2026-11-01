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
    public class DoctorController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DoctorController(AppDbContext context)
        {
            _context = context;
        }

        // POST api/doctor/profile
        // Doctor creates their profile after registration
        [HttpPost("profile")]
        [Authorize(Roles = "Doctor")]
        public async Task<IActionResult> CreateProfile([FromBody] CreateDoctorDto dto)
        {
            // Get logged in doctor's UserId from JWT token
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            // Check if profile already exists
            if (await _context.Doctors.AnyAsync(d => d.UserId == userId))
                return BadRequest("Doctor profile already exists");

            var doctor = new Doctor
            {
                UserId = userId,
                Specialization = dto.Specialization,
                LicenseNumber = dto.LicenseNumber,
                PhoneNumber = dto.PhoneNumber,
                ConsultationFee = dto.ConsultationFee,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Doctors.Add(doctor);
            await _context.SaveChangesAsync();

            return Ok("Doctor profile created successfully");
        }

        // GET api/doctor
        // Get all doctors (anyone can see)
        [HttpGet]
        public async Task<IActionResult> GetAllDoctors([FromQuery] string? specialization = null)
        {
            var query = _context.Doctors.Where(d => d.User.IsActive);
            if (!string.IsNullOrWhiteSpace(specialization))
                query = query.Where(d => EF.Functions.ILike(d.Specialization, $"%{specialization}%"));

            var doctors = await query
                .Select(d => new DoctorResponseDto
                {
                    Id = d.Id,
                    FullName = d.User.FirstName + " " + d.User.LastName,
                    Email = d.User.Email,
                    Specialization = d.Specialization,
                    LicenseNumber = d.LicenseNumber,
                    PhoneNumber = d.PhoneNumber,
                    ConsultationFee = d.ConsultationFee
                })
                .ToListAsync();

            return Ok(doctors);
        }

        // GET api/doctor/myprofile
        // Doctor views their own profile
        [HttpGet("myprofile")]
        [Authorize(Roles = "Doctor")]
        public async Task<IActionResult> GetMyProfile()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var doctor = await _context.Doctors.Include(d => d.User).FirstOrDefaultAsync(d => d.UserId == userId);
            if (doctor == null)
                return NotFound("Doctor profile not found");

            return Ok(new DoctorResponseDto
            {
                Id = doctor.Id,
                FullName = $"{doctor.User.FirstName} {doctor.User.LastName}",
                Email = doctor.User.Email,
                Specialization = doctor.Specialization,
                LicenseNumber = doctor.LicenseNumber,
                PhoneNumber = doctor.PhoneNumber,
                ConsultationFee = doctor.ConsultationFee
            });
        }

        // GET api/doctor/5
        // Get single doctor by id
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetDoctor(int id)
        {
            var doctor = await _context.Doctors
                .Include(d => d.User)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (doctor == null)
                return NotFound("Doctor not found");

            return Ok(new DoctorResponseDto
            {
                Id = doctor.Id,
                FullName = $"{doctor.User.FirstName} {doctor.User.LastName}",
                Email = doctor.User.Email,
                Specialization = doctor.Specialization,
                LicenseNumber = doctor.LicenseNumber,
                PhoneNumber = doctor.PhoneNumber,
                ConsultationFee = doctor.ConsultationFee
            });
        }

        // PUT api/doctor/profile
        // Doctor updates their own profile
        [HttpPut("profile")]
        [Authorize(Roles = "Doctor")]
        public async Task<IActionResult> UpdateProfile([FromBody] CreateDoctorDto dto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var doctor = await _context.Doctors
                .FirstOrDefaultAsync(d => d.UserId == userId);

            if (doctor == null)
                return NotFound("Doctor profile not found");

            doctor.Specialization = dto.Specialization;
            doctor.LicenseNumber = dto.LicenseNumber;
            doctor.PhoneNumber = dto.PhoneNumber;
            doctor.ConsultationFee = dto.ConsultationFee;
            doctor.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok("Doctor profile updated successfully");
        }

        // DELETE api/doctor/5
        // Admin deletes a doctor
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteDoctor(int id)
        {
            var doctor = await _context.Doctors.FindAsync(id);

            if (doctor == null)
                return NotFound("Doctor not found");

            // Soft delete: deactivate the account so appointment history stays intact
            var user = await _context.Users.FindAsync(doctor.UserId);
            if (user != null) user.IsActive = false;
            await _context.SaveChangesAsync();

            return Ok("Doctor deactivated successfully");
        }
    }
}