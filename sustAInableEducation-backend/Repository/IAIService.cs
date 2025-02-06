using sustAInableEducation_backend.Models;

namespace sustAInableEducation_backend.Repository
{
    public interface IAIService
    {
        Task<(StoryPart, string)> StartStory(Story story);
        Task<StoryPart> GenerateNextPart(Story story);
        Task<string> GenerateStoryImage(Story story);
        Task<StoryResult> GenerateResult(Story story);
        Task<Quiz> GenerateQuiz(Story story, QuizRequest config);
        Task<string> GenerateProfileImage(string userName, ImageStyle style);
    }
}
