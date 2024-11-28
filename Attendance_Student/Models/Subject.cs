﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Attendance_Student.Models
{
    public class Subject
    {
        [Key]
        public int subject_Id { get; set; }
        [Required]
        [StringLength(50)]
        public string subject_Name { get; set; }

        public int subject_Duration { get; set; }

        public virtual List<Teacher> teachers { get; set; } = new List<Teacher>();
        public virtual List<SubjectDaySchedule> daysScheduled { get; set; } = new List<SubjectDaySchedule>();


    }
}
