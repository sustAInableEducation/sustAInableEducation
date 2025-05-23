﻿using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using sustAInableEducation_backend.Models;
using sustAInableEducation_backend.Repository;

namespace sustAInableEducation_backend.Controllers;

[Route("[controller]")]
[ApiController]
[Authorize]
public class QuizzesController : ControllerBase
{
    private readonly IAIService _ai;
    private readonly ApplicationDbContext _context;
    private readonly string _userId;

    public QuizzesController(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor, IAIService ai)
    {
        _context = context;
        _userId = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        _ai = ai;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Quiz>>> GetQuizzes(Guid? spaceId)
    {
        if (spaceId != null)
            return await _context.Quiz.Where(q => q.UserId == _userId && q.SpaceId == spaceId).ToListAsync();

        return await _context.Quiz.Where(q => q.UserId == _userId).ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Quiz>> GetQuiz(Guid id)
    {
        var quiz = await _context.Quiz.Include(q => q.Questions)
            .ThenInclude(q => q.Choices)
            .Include(q => q.Questions)
            .ThenInclude(q => q.Results)
            .FirstOrDefaultAsync(q => q.Id == id);

        if (quiz == null) return NotFound();

        if (quiz.UserId != _userId) return Unauthorized();

        return quiz;
    }

    [HttpPost]
    public async Task<ActionResult<Quiz>> PostQuiz(QuizRequest config)
    {
        if (!await _context.IsParticipant(_userId, config.SpaceId)) return Unauthorized();

        var story = (await _context.SpaceWithStory.FirstOrDefaultAsync(e => e.Id == config.SpaceId))!.Story;
        var quiz = await _ai.GenerateQuiz(story, config);
        quiz.UserId = _userId;
        quiz.SpaceId = config.SpaceId;
        _context.Quiz.Add(quiz);
        await _context.SaveChangesAsync();

        return CreatedAtAction("GetQuiz", new { id = quiz.Id }, quiz);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteQuiz(Guid id)
    {
        var quiz = await _context.QuizWithAll.FirstOrDefaultAsync(q => q.Id == id);
        if (quiz == null) return NotFound();

        if (quiz.UserId != _userId) return Unauthorized();

        _context.Quiz.Remove(quiz);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("{id}/try")]
    public async Task<ActionResult<ICollection<QuizResult>>> PostTry(Guid id,
        ICollection<QuizQuestionResponse> responses)
    {
        var quiz = await _context.QuizWithAll.FirstOrDefaultAsync(q => q.Id == id);
        if (quiz == null) return NotFound();

        if (quiz.UserId != _userId) return Unauthorized();

        if (responses.Select(r => r.QuestionId).Intersect(quiz.Questions.Select(q => q.Id)).Count() !=
            responses.Count)
            return BadRequest();

        var tryNumber = quiz.Tries.Count();
        var results = new List<QuizResult>();
        foreach (var question in quiz.Questions)
        {
            var response = responses.FirstOrDefault(r => r.QuestionId == question.Id);
            if (response == null) return BadRequest();

            var result = new QuizResult
            {
                QuizQuestionId = question.Id,
                TryNumber = tryNumber,
                IsCorrect = !question.Choices
                    .Any(c => (c.IsCorrect && !response.Response.Contains(c.Number)) ||
                              (!c.IsCorrect && response.Response.Contains(c.Number)))
            };
            results.Add(result);
            _context.QuizResult.Add(result);
        }

        await _context.SaveChangesAsync();
        return results;
    }
}