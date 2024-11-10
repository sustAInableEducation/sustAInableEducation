﻿using System.ComponentModel.DataAnnotations;

namespace sustAInableEducation_backend.Models
{
    public class StoryChoice
    {
        public Guid StoryPartId { get; set; }
        public int Number { get; set; }
        [MaxLength(2048)]
        public string Text { get; set; }
        public bool IsTaken { get; set; }
    }
}
