﻿using System.ComponentModel.DataAnnotations;

namespace sustAInableEducation_backend.Models
{
    public class Story
    {
        public Guid Id { get; set; }

        public StoryPreset? Preset { get; set; }
        public ICollection<StoryPart> Parts { get; set; } = new List<StoryPart>();

        [MaxLength(256)]
        public string Title { get; set; }
        [MaxLength(1024)]
        public string Prompt { get; set; }
        public int Length { get; set; }
        public int Creativity { get; set; }
    }
}
