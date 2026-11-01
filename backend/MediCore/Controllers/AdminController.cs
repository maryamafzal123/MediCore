using MediCore.Data;
using MediCore.DTOs;
using MediCore.Models;
using MediCore.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MediCore.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly HospitalSchedule _schedule;

        public AdminController(AppDbContext context, HospitalSchedule schedule)
        {
            _context = context;
            _schedule = schedule;
        }

        // GET api/admin/stats — numbers for the admin dashboard
        [HttpGet("stats")]
        public async Task<IActionResult> Stats()
        {
            var todayLocal = DateOnly.FromDateTime(_schedule.ToLocal(DateTime.UtcNow));
            var todayStart = _schedule.LocalToUtc(todayLocal.ToDateTime(TimeOnly.MinValue));
            var weekStart = todayStart.AddDays(-6);

            var byStatus = await _context.Appointments
                .GroupBy(a => a.Status)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToListAsync();

            var bySpec = await _context.Doctors
                .Where(d => d.User.IsActive)
                .GroupBy(d => d.Specialization)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToListAsync();

            var recentDates = await _context.Appointments
                .Where(a => a.AppointmentDate >= weekStart && a.AppointmentDate < todayStart.AddDays(1))
                .Select(a => a.AppointmentDate)
                .ToListAsync();

            var last7 = Enumerable.Range(0, 7).Select(i =>
            {
                var day = todayLocal.AddDays(i - 6);
                return new DailyCountDto
                {
                    Date = day.ToString("yyyy-MM-dd"),
                    Count = recentDates.Count(d => DateOnly.FromDateTime(_schedule.ToLocal(d)) == day)
                };
            }).ToList();

            return Ok(new AdminStatsDto
            {
                TotalDoctors = await _context.Doctors.CountAsync(d => d.User.IsActive),
                TotalPatients = await _context.Patients.CountAsync(),
                TotalAppointments = await _context.Appointments.CountAsync(),
                TodayAppointments = await _context.Appointments.CountAsync(a =>
                    a.AppointmentDate >= todayStart && a.AppointmentDate < todayStart.AddDays(1)),
                TotalPrescriptions = await _context.Prescriptions.CountAsync(),
                AppointmentsByStatus = Enum.GetValues<AppointmentStatus>()
                    .ToDictionary(s => s.ToString(), s => byStatus.FirstOrDefault(x => x.Key == s)?.Count ?? 0),
                DoctorsBySpecialization = bySpec.ToDictionary(x => x.Key, x => x.Count),
                Last7Days = last7
            });
        }

        // GET api/admin/users
        [HttpGet("users")]
        public async Task<IActionResult> Users()
        {
            var users = await _context.Users
                .OrderByDescending(u => u.CreatedAt)
                .Select(u => new UserListDto
                {
                    Id = u.Id,
                    FullName = u.FirstName + " " + u.LastName,
                    Email = u.Email,
                    Role = u.Role.ToString(),
                    IsActive = u.IsActive,
                    CreatedAt = u.CreatedAt
                })
                .ToListAsync();
            return Ok(users);
        }

        // PUT api/admin/users/5/toggle — activate / deactivate an account
        [HttpPut("users/{id:int}/toggle")]
        public async Task<IActionResult> ToggleUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound("User not found");
            if (user.Id == User.GetUserId()) return BadRequest("You cannot deactivate your own account");

            user.IsActive = !user.IsActive;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return Ok(new { user.Id, user.IsActive });
        }
    }
}
