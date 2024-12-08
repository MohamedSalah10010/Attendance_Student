using Attendance_Student.DTOs.AttendanceDTOs;
using Attendance_Student.DTOs.StudentDTO;
using Attendance_Student.Models;
using Attendance_Student.UnitOfWorks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.Annotations;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Attendance_Student.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AttendanceController : ControllerBase
    {
        UnitWork _unit;
        UserManager<IdentityUser> _userManager;
        public AttendanceController(UnitWork unit, UserManager<IdentityUser> userManager)
        {
            _unit = unit;
            _userManager = userManager;
        }
        [HttpPost("class/{class_id}")]
        [SwaggerOperation(Summary = "Record attendance for a class", Description = "Records attendance for a specific class.")]
        [SwaggerResponse(StatusCodes.Status200OK, "Attendance recorded successfully.")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid class ID or attendance data.")]
        public async Task<IActionResult> RecordAttendance(int class_id, AttendanceDTO attendanceDTO)
        {
            if (attendanceDTO == null) return BadRequest("Attendance data is required.");

            Class _class = await _unit.ClassRepo.selectById(class_id);
            if (_class == null) return BadRequest($"Class with ID {class_id} not found.");

            if (_class.timeTable == null)
            {
                return BadRequest("Class is not associated with any timetable.");
            }

            List<StudentAttendance> studentAttendances = new List<StudentAttendance>();

            var teacher = await _unit.TeacherRepo.selectUserById(attendanceDTO.Teacher_id);
            if (teacher == null)
            {
                return BadRequest($"Teacher with ID {attendanceDTO.Teacher_id} not found.");
            }
            foreach (StudentAttendanceDTO studentAttendanceDTO in attendanceDTO.Students)
            {
                var st = await _unit.StudentRepo.selectUserById(studentAttendanceDTO.StudentId);
                if (st == null)
                    return BadRequest($"Student with ID {studentAttendanceDTO.StudentId} not found.");

                StudentAttendance student = new StudentAttendance()
                {
                    Status = studentAttendanceDTO.status,
                    StudentId = studentAttendanceDTO.StudentId,
                    student = st
                };
                studentAttendances.Add(student);
            }
            Attendance attendance = new Attendance()
            {
                dateAttendance = attendanceDTO.DateAttendance,
                Feedback = attendanceDTO.Feedback,
                TimeTableId = _class.timeTable.TimeTableId,
                teacher_id = attendanceDTO.Teacher_id,
                StudentsAttendance = studentAttendances

            };

            await _unit.AttendanceRepo.add(attendance);
            await _unit.Save();
            List<SelectStudentDTO> STs = new List<SelectStudentDTO>();
            foreach (var st in attendance.StudentsAttendance)
            {
                SelectStudentDTO student = new SelectStudentDTO()
                {
                    username = st.student.UserName,
                    status = st.Status
                };
                STs.Add(student);
            }
            SelectAttendanceDTO selectAttendance = new SelectAttendanceDTO()
            {
                Id = attendance.Id,
                Feedback = attendance.Feedback,
                dateAttendance = attendance.dateAttendance,
                StudentsAttendance = STs,
                teacher_name = teacher == null ? "" : teacher.Teacher_fullName,
                subject_name = teacher.Subject == null ? "" : teacher.Subject.subject_Name
            };
            return Ok(selectAttendance);
        }

        [HttpGet("class/{class_id}/date/{date}")]
        [SwaggerOperation(Summary = "Get attendance for a class on a specific date", Description = "Retrieves attendance records for a specific class on a given date.")]
        [SwaggerResponse(StatusCodes.Status200OK, "Attendance records retrieved successfully.")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "No attendance records found.")]
        public async Task<IActionResult> GetAttendanceDay(int class_id, DateOnly date)
        {
            Class _class = await _unit.ClassRepo.selectById(class_id);

            if (_class == null) return BadRequest($"Class with ID {class_id} not found.");
            List<Attendance> attendances = await _unit.AttendanceRepository.GetAttendancesDay(class_id , date);
            if (!attendances.Any()) return NotFound("No attendance records found for the specified day.");
            List<SelectAttendanceDTO> attendanceDTOs = new List<SelectAttendanceDTO>();
            foreach (var attendance in attendances)
            {
                List<SelectStudentDTO> studentDTOs = new List<SelectStudentDTO>();
                foreach (var student in attendance.StudentsAttendance)
                {
                    SelectStudentDTO studentDTO = new SelectStudentDTO()
                    {
                        id = student.StudentId,
                        username = student.student.Student_fullname,
                        status = student.Status
                    };
                    studentDTOs.Add(studentDTO);
                }
                SelectAttendanceDTO attendanceDTO = new SelectAttendanceDTO()
                {
                    Id = attendance.Id,
                    Feedback = attendance.Feedback,
                    dateAttendance = attendance.dateAttendance,
                    teacher_name = attendance.teacher.Teacher_fullName,
                    subject_name = attendance.teacher.Subject.subject_Name,
                    StudentsAttendance = studentDTOs
                };
                attendanceDTOs.Add(attendanceDTO);
            }
            return Ok(attendanceDTOs);
        }
        [HttpGet("/student/{student_id}")]
        [SwaggerOperation(Summary = "Get attendance for a student", Description = "Retrieves attendance records for a specific student.")]
        [SwaggerResponse(StatusCodes.Status200OK, "Attendance records retrieved successfully.")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "No attendance records found.")]
        public async Task<IActionResult> GetAttendanceStudent(string student_id)
        {
            Student _student = await _unit.StudentRepo.selectUserById(student_id);

            if (_student == null) return BadRequest($"Student with ID {student_id} not found.");
            var attendances_Student = await _unit.AttendanceRepository.GetAttendanceStudent(student_id);
            if (!attendances_Student.Any()) return NotFound($"No attendance records found for student ID {student_id}.");
            List<SelectAttendaceStudentDTO> attendances_studentDTO = new List<SelectAttendaceStudentDTO>();
            foreach (var attendanceST in attendances_Student)
            {
                SelectAttendaceStudentDTO selectAttendanceStudent = new SelectAttendaceStudentDTO()
                {
                    Id = attendanceST.AttendanceId,
                    dateAttendance = attendanceST.attendance.dateAttendance,
                    teacher_name = attendanceST.attendance.teacher.Teacher_fullName,
                    subject_name = attendanceST.attendance.teacher.Subject.subject_Name,
                    status = attendanceST.Status
                };
                attendances_studentDTO.Add(selectAttendanceStudent);
            }
            return Ok(attendances_studentDTO);


        }

        [HttpGet("report/class/{class_id}/range/{start_date}/{end_date}")]
        [SwaggerOperation(Summary = "Get attendance report for a class", Description = "Retrieves attendance report for a specific class within a date range.")]
        [SwaggerResponse(StatusCodes.Status200OK, "Attendance report retrieved successfully.")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "No attendance records found.")]
        public async Task<IActionResult> GetAttendanceClassReport(int class_id, DateOnly start_date, DateOnly end_date)
        {
            Class _class = await _unit.ClassRepo.selectById(class_id);

            if (_class == null) return BadRequest($"Class with ID {class_id} not found.");
            List<Attendance> attendances = await _unit.AttendanceRepository.GetAttendancesClass(class_id, start_date, end_date);
            if (!attendances.Any()) return NotFound("No attendance records found for the specified day.");
            List<SelectAttendanceDTO> attendanceDTOs = new List<SelectAttendanceDTO>();
            foreach (var attendance in attendances)
            {
                List<SelectStudentDTO> studentDTOs = new List<SelectStudentDTO>();
                foreach (var student in attendance.StudentsAttendance)
                {
                    SelectStudentDTO studentDTO = new SelectStudentDTO()
                    {
                        id = student.StudentId,
                        username = student.student.Student_fullname,
                        status = student.Status
                    };
                    studentDTOs.Add(studentDTO);
                }
                SelectAttendanceDTO attendanceDTO = new SelectAttendanceDTO()
                {
                    Id = attendance.Id,
                    Feedback = attendance.Feedback,
                    dateAttendance = attendance.dateAttendance,
                    teacher_name = attendance.teacher.Teacher_fullName,
                    subject_name = attendance.teacher.Subject.subject_Name,
                    StudentsAttendance = studentDTOs
                };
                attendanceDTOs.Add(attendanceDTO);
            }
            return Ok(attendanceDTOs);
        }
        [HttpGet("report/student/{student_id}/range/{start_date}/{end_date}")]
        [SwaggerOperation(Summary = "Get attendance report for a student",
                  Description = "Retrieves attendance report for a specific student within a specified date range.")]
        [SwaggerResponse(StatusCodes.Status200OK, "Attendance report retrieved successfully.")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "No attendance records found.")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid student ID.")]
        public async Task<IActionResult> GetAttendanceStudentReport(string student_id, DateOnly start_date, DateOnly end_date)
        {
            Student _student = await _unit.StudentRepo.selectUserById(student_id);

            if (_student == null) return BadRequest($"Student with ID {student_id} not found.");
            var attendances_Student = await _unit.AttendanceRepository.GetAttendanceStudentRange(student_id, start_date, end_date);
            if (!attendances_Student.Any()) return NotFound($"No attendance records found for student ID {student_id}.");
            List<SelectAttendaceStudentDTO> attendances_studentDTO = new List<SelectAttendaceStudentDTO>();
            foreach (var attendanceST in attendances_Student)
            {
                SelectAttendaceStudentDTO selectAttendanceStudent = new SelectAttendaceStudentDTO()
                {
                    Id = attendanceST.AttendanceId,
                    dateAttendance = attendanceST.attendance.dateAttendance,
                    teacher_name = attendanceST.attendance.teacher.Teacher_fullName,
                    subject_name = attendanceST.attendance.teacher.Subject.subject_Name,
                    status = attendanceST.Status
                };
                attendances_studentDTO.Add(selectAttendanceStudent);
            }
            return Ok(attendances_studentDTO);


        }
    }
}