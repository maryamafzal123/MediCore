using MediCore.Models;
using Microsoft.EntityFrameworkCore;

namespace MediCore.Data
{
    // Inserts demo data on first run so the app (and the chatbot) has something to show.
    // All demo passwords: Password@123
    public static class DbSeeder
    {
        public static async Task SeedAsync(AppDbContext db)
        {
            // Skip if demo data was already added (your own test users are left untouched)
            if (await db.Users.AnyAsync(u => u.Email == "admin@medicore.com")) return;

            var hash = BCrypt.Net.BCrypt.HashPassword("Password@123");
            var now = DateTime.UtcNow;

            User NewUser(string first, string last, string email, UserRole role) => new()
            {
                FirstName = first, LastName = last, Email = email, PasswordHash = hash,
                Role = role, IsActive = true, CreatedAt = now, UpdatedAt = now
            };

            var admin = NewUser("System", "Admin", "admin@medicore.com", UserRole.Admin);
            db.Users.Add(admin);

            var doctors = new List<Doctor>
            {
                new() { User = NewUser("Ahmed", "Khan", "ahmed.khan@medicore.com", UserRole.Doctor),
                        Specialization = "Cardiology", LicenseNumber = "PMDC-10231", PhoneNumber = "03001234567", ConsultationFee = 3000 },
                new() { User = NewUser("Sana", "Malik", "sana.malik@medicore.com", UserRole.Doctor),
                        Specialization = "Dermatology", LicenseNumber = "PMDC-10877", PhoneNumber = "03011234567", ConsultationFee = 2500 },
                new() { User = NewUser("Bilal", "Hussain", "bilal.hussain@medicore.com", UserRole.Doctor),
                        Specialization = "General Medicine", LicenseNumber = "PMDC-11452", PhoneNumber = "03021234567", ConsultationFee = 1500 },
                new() { User = NewUser("Ayesha", "Raza", "ayesha.raza@medicore.com", UserRole.Doctor),
                        Specialization = "Pediatrics", LicenseNumber = "PMDC-12008", PhoneNumber = "03031234567", ConsultationFee = 2000 },
                new() { User = NewUser("Usman", "Tariq", "usman.tariq@medicore.com", UserRole.Doctor),
                        Specialization = "Orthopedics", LicenseNumber = "PMDC-12590", PhoneNumber = "03041234567", ConsultationFee = 3500 },
            };
            db.Doctors.AddRange(doctors);

            var patients = new List<Patient>
            {
                new() { User = NewUser("Ali", "Raza", "patient@medicore.com", UserRole.Patient),
                        PhoneNumber = "03111234567", DateOfBirth = new DateTime(1998, 4, 12, 0, 0, 0, DateTimeKind.Utc),
                        Gender = "Male", Address = "Model Town, Lahore", BloodGroup = "B+" },
                new() { User = NewUser("Fatima", "Noor", "fatima.noor@medicore.com", UserRole.Patient),
                        PhoneNumber = "03121234567", DateOfBirth = new DateTime(1990, 9, 3, 0, 0, 0, DateTimeKind.Utc),
                        Gender = "Female", Address = "Gulberg, Lahore", BloodGroup = "O+" },
            };
            db.Patients.AddRange(patients);
            await db.SaveChangesAsync();

            // Appointment times on the hour, Pakistan time (UTC+5) → stored in UTC
            DateTime At(int daysFromToday, int localHour) =>
                DateTime.SpecifyKind(DateTime.UtcNow.Date.AddDays(daysFromToday).AddHours(localHour - 5), DateTimeKind.Utc);

            var past = new Appointment { Doctor = doctors[2], Patient = patients[0], AppointmentDate = At(-7, 10),
                                         Notes = "Fever and sore throat", Status = AppointmentStatus.Completed };
            db.Appointments.AddRange(
                past,
                new Appointment { Doctor = doctors[0], Patient = patients[0], AppointmentDate = At(2, 11),
                                  Notes = "Routine heart checkup", Status = AppointmentStatus.Scheduled },
                new Appointment { Doctor = doctors[1], Patient = patients[1], AppointmentDate = At(1, 14),
                                  Notes = "Skin allergy", Status = AppointmentStatus.Scheduled },
                new Appointment { Doctor = doctors[2], Patient = patients[1], AppointmentDate = At(-3, 12),
                                  Notes = "Headache", Status = AppointmentStatus.Cancelled }
            );
            await db.SaveChangesAsync();

            db.Prescriptions.Add(new Prescription
            {
                Appointment = past, Patient = patients[0],
                Diagnosis = "Viral pharyngitis",
                Medications = "Paracetamol 500mg - 1 tablet every 8 hours for 3 days; Lozenges as needed",
                Instructions = "Drink warm fluids, rest, return if fever persists beyond 3 days",
                ValidUntil = DateTime.UtcNow.AddDays(30)
            });
            await db.SaveChangesAsync();
        }
    }
}
