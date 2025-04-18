﻿using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using sustAInableEducation_backend.Models.Validation;

namespace sustAInableEducation_backend.Models;

public class Quiz
{
    public Guid Id { get; set; }
    [JsonIgnore] public string UserId { get; set; } = null!;
    public Guid SpaceId { get; set; }

    public ICollection<QuizQuestion> Questions { get; set; } = new List<QuizQuestion>();

    [MaxLength(256)] public string Title { get; set; } = null!;
    public uint NumberQuestions { get; set; }

    public IEnumerable<IGrouping<int, QuizResult>> Tries =>
        Questions.Select(q => q.Results).SelectMany(r => r).GroupBy(r => r.TryNumber);
}

public enum QuizType
{
    SingleResponse,
    MultipleResponse,
    TrueFalse
}

public class QuizRequest
{
    public Guid SpaceId { get; set; }

    [ValidEnum] public ICollection<QuizType> Types { get; set; } = new List<QuizType>();

    [Range(1, 20)] public uint NumberQuestions { get; set; }
}