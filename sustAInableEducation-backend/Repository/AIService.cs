﻿using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using SkiaSharp;
using sustAInableEducation_backend.Models;

namespace sustAInableEducation_backend.Repository;

public class AIService : IAIService
{
    private const int MaxRetryAttempts = 2;
    private const string TextGenerationModel = "meta-llama/Llama-4-Maverick-17B-128E-Instruct-FP8";
    private const string ImagePromptModel = "meta-llama/Llama-4-Maverick-17B-128E-Instruct-FP8";

    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.Latin1Supplement),
    };

    private readonly HttpClient _client;
    private readonly ILogger _logger;

    public AIService(IConfiguration config, ILogger<AIService> logger)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
        try
        {
            _client = new HttpClient
            {
                BaseAddress = new Uri(config["DeepInfra:Url"] ??
                                      throw new ArgumentNullException(nameof(config))),
                Timeout = TimeSpan.FromMinutes(2.5)
            };
            _client.DefaultRequestHeaders.Add("Authorization",
                $"Bearer {config["DeepInfra:ApiKey"] ?? throw new ArgumentNullException(nameof(config))}");
        }
        catch (Exception e)
        {
            _logger.LogCritical("Failed to initialise AI service: {Exception}", e);
            throw;
        }

        _logger.LogInformation("AI service initialised");
    }

    // Benjamin Edlinger
    /// <summary>
    ///     Starts a new story with the given story object
    /// </summary>
    /// <param name="story"> The story object to start</param>
    /// <returns>The first part of the story and the title of the story</returns>
    /// <exception cref="ArgumentException">If the story object is invalid</exception>
    /// <exception cref="AIException">
    ///     If the story could not be started due to an error while fetching the content or
    ///     deserializing
    /// </exception>
    public async Task<(StoryPart, string)> StartStory(Story story)
    {
        ArgumentNullException.ThrowIfNull(story);

        _logger.LogInformation("Starting new story with id: {Id}", story.Id);
        List<ChatMessage> chatMessages;
        try
        {
            chatMessages = RebuildChatMessagesStory(story);
        }
        catch (Exception e)
        {
            _logger.LogError("Failed to rebuild chat messages because of error in story object: {Exception}", e);
            throw new ArgumentException("Failed to rebuild chat messages because of error in story object", e);
        }

        var attempt = 0;
        while (attempt < MaxRetryAttempts)
            try
            {
                var assistantContent = await FetchAssistantContent(chatMessages, story.Temperature, story.TopP);
                var (storyPart, title) = await GetStoryPart(assistantContent, story.TargetGroup, chatMessages);
                _logger.LogInformation("Successfully started story with id: {Id}", story.Id);
                return (storyPart, title);
            }
            catch (Exception e)
            {
                _logger.LogError("Failed to start the story on attempt {Number}: {Exception}", attempt + 1, e);
                if (attempt >= MaxRetryAttempts - 1)
                {
                    _logger.LogError("Reached maximum retry attempts for trying to start the story");
                    throw new AIException("Reached maximum retry attempts for trying to start the story", e);
                }

                attempt++;
            }

        throw new AIException("Failed to start story after maximum retry attempts");
    }

    // Benjamin Edlinger
    /// <summary>
    ///     Generates the next part of the story based on the given story object
    /// </summary>
    /// <param name="story">The story object to generate the next part for</param>
    /// <returns>The next part of the story</returns>
    /// <exception cref="ArgumentException">If the story object is invalid</exception>
    /// <exception cref="AIException">
    ///     If the next part could not be generated due to an error while fetching the content or
    ///     deserializing
    /// </exception>
    public async Task<StoryPart> GenerateNextPart(Story story)
    {
        ArgumentNullException.ThrowIfNull(story);

        _logger.LogInformation("Generating next part story with id: {Id}", story.Id);
        List<ChatMessage> chatMessages;
        try
        {
            chatMessages = RebuildChatMessagesStory(story);
        }
        catch (Exception e)
        {
            _logger.LogError("Failed to rebuild chat messages because of error in story object: {Exception}", e);
            throw new ArgumentException("Failed to rebuild chat messages because of error in story object", e);
        }

        var attempt = 0;
        while (attempt < MaxRetryAttempts)
            try
            {
                var assistantContent = await FetchAssistantContent(chatMessages, story.Temperature, story.TopP);
                var (storyPart, _) = await GetStoryPart(assistantContent, story.TargetGroup, chatMessages);
                _logger.LogInformation("Successfully generated next part of story with id: {Id}", story.Id);
                return storyPart;
            }
            catch (Exception e)
            {
                _logger.LogError("Failed to generate next part on attempt {Number}: {Exception}", attempt + 1, e);
                if (attempt >= MaxRetryAttempts - 1)
                {
                    _logger.LogError("Reached maximum retry attempts for trying to generate next part");
                    throw new AIException("Reached maximum retry attempts for trying to generate next part", e);
                }

                attempt++;
            }

        throw new AIException("Failed to generate next part after maximum retry attempts");
    }

    // Benjamin Edlinger
    /// <summary>
    ///     Generates the result of the story based on the given story object
    /// </summary>
    /// <param name="story">The story object to generate the result for</param>
    /// <returns>The result of the story</returns>
    /// <exception cref="ArgumentException">If the story object is invalid</exception>
    /// <exception cref="AIException">
    ///     If the result could not be generated due to an error while fetching the content or
    ///     deserializing
    /// </exception>
    public async Task<StoryResult> GenerateResult(Story story)
    {
        ArgumentNullException.ThrowIfNull(story);

        _logger.LogInformation("Generating result of story with id: {Id}", story.Id);
        List<ChatMessage> chatMessages;
        try
        {
            chatMessages = RebuildChatMessagesStory(story);
        }
        catch (Exception e)
        {
            _logger.LogError("Failed to rebuild chat messages because of error in story object: {Exception}", e);
            throw new ArgumentException("Failed to rebuild chat messages because of error in story object", e);
        }

        string end = null!;
        var attempt = 0;
        while (attempt < MaxRetryAttempts)
            try
            {
                var assistantContent = await FetchAssistantContent(chatMessages, story.Temperature, story.TopP);
                var (storyPart, _) = await GetStoryPart(assistantContent, story.TargetGroup, chatMessages);
                end = storyPart.Text;
                break;
            }
            catch (Exception e)
            {
                _logger.LogError("Failed to get the end part of the story on attempt {Number}: {Exception}",
                    attempt + 1, e);
                if (attempt >= MaxRetryAttempts - 1)
                {
                    _logger.LogError("Reached maximum retry attempts for trying to get the end part of the story");
                    throw new AIException(
                        "Reached maximum retry attempts for trying to get the end part of the story", e);
                }

                attempt++;
            }

        try
        {
            chatMessages = RebuildChatMessagesResult(story, chatMessages, end);
        }
        catch (Exception e)
        {
            _logger.LogError(
                "Failed to rebuild chat messages for result because of error in story object: {Exception}", e);
            throw new ArgumentException(
                "Failed to rebuild chat messages for result because of error in story object", e);
        }

        attempt = 0;
        while (attempt < MaxRetryAttempts)
            try
            {
                var assistantContent = await FetchAssistantContent(chatMessages, story.Temperature, story.TopP);
                var result = GetStoryResult(assistantContent, end);
                _logger.LogInformation("Successfully generated result of story with title {Title}", story.Title);
                return result;
            }
            catch (Exception e)
            {
                _logger.LogError("Failed to generate result on attempt {Number}: {Exception}", attempt + 1, e);
                if (attempt >= MaxRetryAttempts - 1)
                {
                    _logger.LogError("Reached maximum retry attempts for trying to generate result");
                    throw new AIException("Reached maximum retry attempts for trying to generate result", e);
                }

                attempt++;
            }

        throw new AIException("Failed to generate result after maximum retry attempts");
    }

    // Benjamin Edlinger
    /// <summary>
    ///     Generates an image based on the given story object
    /// </summary>
    /// <param name="story">Based on this story object the image will be generated</param>
    /// <returns>The path to the generated image</returns>
    /// <exception cref="AIException">
    ///     If the request for generating the prompt failed, the request for generating the image
    ///     failed, the response content could not be deserialized or the image could not be saved
    /// </exception>
    /// <exception cref="InvalidOperationException">
    ///     If the response object is null, the assistant content is null or empty, the
    ///     image content is not a base64 string or the image content is null
    /// </exception>
    public async Task<string> GenerateStoryImage(Story story)
    {
        ArgumentNullException.ThrowIfNull(_client);
        ArgumentNullException.ThrowIfNull(story);

        _logger.LogInformation("Generating image for story with id: {Id}", story.Id);

        var systemPrompt =
            "Du bist ein professioneller Prompt Engineer, der sich auf die Erstellung hochdetaillierter und konsistenter Bildbeschreibungen für KI-basierte Bildgenerierungssysteme spezialisiert hat. Deine Aufgabe ist es, Prompts zu entwerfen, die zu visuell harmonischen und stilistisch kohärenten Bildern führen. Achte darauf, dass die Beschreibung lebendig und spezifisch ist und alle relevanten Details umfasst – dazu gehören:"
            + "- Setting: Ort, Umgebung und atmosphärische Details"
            + "- Charaktere: Aussehen, Mimik, Kleidung und Ausdruck"
            + "- Beleuchtung und Farben: Lichtverhältnisse, Farbschema, Stimmung"
            + "- Stimmung und Atmosphäre: Emotionale Wirkung und erzählerischer Ton"
            + "- Künstlerischer Stil: Angabe von Technik, Epoche oder Medium (z.B. „ein surrealistisches Ölgemälde des 19. Jahrhunderts“ oder „eine cinematische, photorealistische Szene mit weichem Licht“)"
            + "Wichtig: Alle Elemente müssen im Einklang mit der Erzählung der Geschichte stehen, ohne widersprüchliche oder unpassende Stilmittel."
            + "Zusätzlich ist der künstlerische Stil an die Zielgruppe anzupassen:"
            + "- Volksschüler (6-10 Jahre): Verwende einen cartoonhaften, verspielten Stil mit kräftigen Primärfarben, einfachen Formen und freundlichen, fantasievollen Charakteren."
            + "- Schüler der Sekundarstufe eins (11-14 Jahre): Wähle einen halb-realistischen oder stilisierten Stil mit lebendigen, klaren Farben und dynamischen Kompositionen, der Abenteuer und leichte Dramatik vermittelt."
            + "- Schüler der Sekundarstufe zwei (15-19 Jahre): Setze auf einen detaillierten, photorealistischen Stil mit anspruchsvoller Beleuchtung, realistischen Texturen und einer ernsten, nachdenklichen Atmosphäre."
            + "Dein Ziel ist es, prägnante, kreative und präzise Prompts zu erstellen, die die KI optimal anleiten, beeindruckende Bilder zu generieren.";
        var text = story.Result != null ? story.Result.Text : story.Parts.Last().Text;
        ArgumentException.ThrowIfNullOrEmpty(text);
        var targetStyle = story.TargetGroup switch
        {
            TargetGroup.PrimarySchool =>
                "- Für Volksschüler: cartoonhaft, verspielt und mit kräftigen Primärfarben.",
            TargetGroup.MiddleSchool =>
                "- Für Sekundarstufe eins: halb-realistischer/stilisierter Stil, lebendig und dynamisch.",
            TargetGroup.HighSchool => "- Für Sekundarstufe zwei: detailliert, photorealistisch und ernst.",
            _ => throw new ArgumentException("Invalid target group")
        };
        var userPrompt =
            $"Erstelle einen detaillierten und lebendigen Prompt für ein KI-basiertes Bildgenerierungssystem basierend auf folgendem Storypart: \"{text}\"."
            + "Bitte stelle sicher, dass:"
            + "- Alle relevanten Details wie Setting, Charaktere, Beleuchtung, Farben, Stimmung und künstlerischer Stil in der Bildbeschreibung enthalten sind."
            + "- Die Bildbeschreibung vollständig mit der Erzählung übereinstimmt und alle Elemente stilistisch harmonisch aufeinander abgestimmt sind."
            + "- Der künstlerische Stil passgenau an die Zielgruppe angepasst wird:"
            + targetStyle
            + "- Du eine klare, präzise und bildhafte Sprache verwendest, um die KI optimal anzuleiten."
            + "Nutze diese Anweisungen, um einen hochwertigen, zielgruppenspezifischen Bildprompt zu generieren."
            + "Antworte nur auf Englisch und nur mit dem Prompt, sonst nichts weiters!";

        List<ChatMessage> chatMessages =
        [
            new() { Role = ValidRoles.System, Content = systemPrompt },
            new() { Role = ValidRoles.User, Content = userPrompt }
        ];

        string imagePrompt;
        try
        {
            _logger.LogInformation("Fetching assistant content for image prompt");
            imagePrompt = await FetchAssistantContent(chatMessages, story.Temperature, story.TopP, false);
            imagePrompt = imagePrompt.Replace("\n", " ");
            imagePrompt = imagePrompt.Replace("\"", "");
        }
        catch (Exception e)
        {
            _logger.LogError("Failed to fetch the image prompt: {Exception}", e);
            throw new AIException("Failed to fetch the image prompt", e);
        }

        HttpRequestMessage requestImage = new(HttpMethod.Post, "/v1/inference/black-forest-labs/FLUX-1-dev")
        {
            Content = new StringContent(JsonSerializer.Serialize(new
            {
                prompt = imagePrompt,
                // approx. 2.67:1 ratio
                width = 1000,
                height = 250
            }), Encoding.UTF8, "application/json")
        };
        HttpResponseMessage responseImage = null!;
        string responseStringImage;
        try
        {
            _logger.LogInformation("Generating image based on prompt");
            responseImage = await _client.SendAsync(requestImage);
            responseImage.EnsureSuccessStatusCode();
            responseStringImage = await responseImage.Content.ReadAsStringAsync();
        }
        catch (HttpRequestException e)
        {
            _logger.LogError("Request for image generation failed with status code {StatusCode}",
                responseImage.StatusCode);
            throw new AIException(
                $"Request for image generation failed with status code {responseImage.StatusCode}", e);
        }

        string base64String;
        try
        {
            var responseObjectImage = JsonSerializer.Deserialize<ImageContent>(responseStringImage) ??
                                      throw new InvalidOperationException("Response object is null");
            base64String = responseObjectImage.Images[0];
            if (base64String.StartsWith("data:image/png;base64,"))
            {
                base64String = base64String.Replace("data:image/png;base64,", string.Empty);
            }
            else
            {
                _logger.LogError("Image content is not a base64 string");
                throw new InvalidOperationException("Image content is not a base64 string");
            }
        }
        catch (JsonException e)
        {
            _logger.LogError("Failed to deserialize response content: {Exception}", e);
            throw new AIException("Failed to deserialize response content", e);
        }

        string folderName, fileName;
        try
        {
            var wwwRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            folderName = "images";
            var directoryPath = Path.Combine(wwwRootPath, folderName);
            if (!Directory.Exists(directoryPath)) Directory.CreateDirectory(directoryPath);

            fileName = $"{Guid.NewGuid()}.png";
            var filePath = Path.Combine(directoryPath, fileName);
            var imageBytes = Convert.FromBase64String(base64String);
            using var ms = new MemoryStream(imageBytes);
            using var skImage = SKImage.FromEncodedData(ms);
            using var skData = skImage.Encode(SKEncodedImageFormat.Png, 100);
            await using var fileStream = File.OpenWrite(filePath);
            skData.SaveTo(fileStream);
        }
        catch (Exception e)
        {
            _logger.LogError("Failed to save image: {Exception}", e);
            throw new AIException("Failed to save image", e);
        }

        _logger.LogInformation("Image generated successfully");
        return Path.Combine("/", folderName, fileName).Replace("\\", "/");
    }

    /// <summary>
    ///     Kacper Bohaczyk
    /// </summary>
    /// <param name="userName"></param>
    /// <returns></returns>
    public async Task<string> GenerateProfileImage(string userName, ImageStyle style)
    {
        ArgumentNullException.ThrowIfNull(_client);
        ArgumentNullException.ThrowIfNull(userName);

        var stringstyle = GetEnumMemberValue(style);

        // String imagePrompt = $"Use the {stringstyle}.  The image should be related to the aspect of sustainability that matches the term '{userName}";
        var imagePrompt =
            $"Generate an image related to the aspect of sustainability that matches the term '{userName}'. Use the {stringstyle}.";
        // String imagePrompt = $"Generate an image related to the aspect of sustainability that matches the term '{userName}Manga – 'Medieval – 'A richly detailed medieval illustration inspired by illuminated manuscripts and old-world paintings. The colors are muted, with ornate patterns, historical clothing, and a sense of ancient storytelling.'";
        //_logger.LogDebug(style.ToString());
        //_logger.LogDebug(imagePrompt);


        // String imagePrompt = $"Generate an image related to the aspect of sustainability that matches the term '{userName}'. Manga – 'A dynamic manga-style illustration with expressive characters, bold linework, and highly detailed backgrounds. The image has a black-and-white or cel-shaded look, with dramatic shading and action-oriented poses.'";
        HttpRequestMessage requestImage = new(HttpMethod.Post, "/v1/inference/black-forest-labs/FLUX-1-dev")
        {
            Content = new StringContent(JsonSerializer.Serialize(new
            {
                prompt = imagePrompt,
                width = 256,
                height = 256
            }), Encoding.UTF8, "application/json")
        };
        HttpResponseMessage responseImage = null!;
        string responseStringImage;

        try
        {
            responseImage = await _client.SendAsync(requestImage);
            responseImage.EnsureSuccessStatusCode();
            responseStringImage = await responseImage.Content.ReadAsStringAsync();
        }
        catch (HttpRequestException e)
        {
            throw new AIException(
                $"Request for image generation failed with status code {responseImage?.StatusCode}", e);
        }

        string base64String;
        try
        {
            var responseObjectImage = JsonSerializer.Deserialize<ImageContent>(responseStringImage) ??
                                      throw new InvalidOperationException("Response object is null");
            base64String = responseObjectImage.Images[0];
            if (base64String.StartsWith("data:image/png;base64,"))
                base64String = base64String.Replace("data:image/png;base64,", string.Empty);
            else
                throw new InvalidOperationException("Image content is not a base64 string");
        }
        catch (JsonException e)
        {
            throw new AIException("Failed to deserialize response content", e);
        }

        string folderName, fileName;
        try
        {
            var wwwRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            folderName = "images";
            var directoryPath = Path.Combine(wwwRootPath, folderName);
            if (!Directory.Exists(directoryPath)) Directory.CreateDirectory(directoryPath);

            fileName = $"{Guid.NewGuid()}.png";
            var filePath = Path.Combine(directoryPath, fileName);
            var imageBytes = Convert.FromBase64String(base64String);
            using (var ms = new MemoryStream(imageBytes))
            {
                using var skImage = SKImage.FromEncodedData(ms);
                using var skData = skImage.Encode(SKEncodedImageFormat.Png, 100);
                using var fileStream = File.OpenWrite(filePath);
                skData.SaveTo(fileStream);
            }
        }
        catch (Exception e)
        {
            throw new AIException("Failed to save image", e);
        }

        return Path.Combine("/", folderName, fileName).Replace("\\", "/");
    }


    /// <summary>
    ///     Kacper Bohaczyk
    /// </summary>
    /// <param name="story"></param>
    /// <param name="config"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public async Task<Quiz> GenerateQuiz(Story story, QuizRequest config)
    {
        ArgumentNullException.ThrowIfNull(story);
        Quiz erg = null;
        List<ChatMessage> chatMessages;

        for (var xer = 0; xer <= 1; xer++)
        {
            try
            {
                chatMessages = RebuildChatMessagesStory(story);
                if (story.Result == null) throw new AIException("The result is not set");
                chatMessages = RebuildChatMessagesResult(story, chatMessages, story.Result.Text);
                chatMessages = RebuildChatMessagesQuiz(story, config, chatMessages);
            }
            catch (Exception e)
            {
                throw new ArgumentException("Failed to rebuild chat messages because of error in story object", e);
            }

            try
            {
                var assistantContent = await FetchAssistantContent(chatMessages, 0.8f, 0.9f);
                erg = GetQuiz(assistantContent);
                break;
            }
            catch (Exception e)
            {
                if (xer == 1) throw new ArgumentException("Failed to getQuiz", e);
            }
        }

        ;
        return erg;
        throw new NotImplementedException();
    }

    // Benjamin Edlinger
    /// <summary>
    ///     Rebuilds the chat messages of the story for the given story object
    /// </summary>
    /// <param name="story">The story object to rebuild the chat messages for</param>
    /// <returns>The rebuilt chat messages</returns>
    /// <exception cref="ArgumentException">If the story object is invalid</exception>
    /// <exception cref="ArgumentNullException">If the story object is null</exception>
    private static List<ChatMessage> RebuildChatMessagesStory(Story story)
    {
        ArgumentNullException.ThrowIfNull(story);

        var targetGroupString = story.TargetGroup switch
        {
            TargetGroup.PrimarySchool =>
                "Die Geschichte wird für Volksschüler (6-10 Jahre) erstellt. Passe Sprachstil, Wortwahl und Inhalte genau an die jeweilige Altersgruppe an:"
                + "- Volksschüler: Verwende einfache Sprache, kurze Sätze und erkläre schwierige Begriffe mit Alltagsbeispielen.",
            TargetGroup.MiddleSchool =>
                "Die Geschichte wird für Schüler der Sekundarstufe eins (10-14 Jahre) erstellt. Passe Sprachstil, Wortwahl und Inhalte genau an die jeweilige Altersgruppe an:"
                + "- Sekundarstufe eins: Nutze lebendige, verständliche Sprache und integriere moralische Konflikte, die greifbar sind.",
            TargetGroup.HighSchool =>
                "Die Geschichte wird für Schüler der Sekundarstufe zwei (15-19 Jahre) erstellt. Passe Sprachstil, Wortwahl und Inhalte genau an die jeweilige Altersgruppe an:"
                + "- Sekundarstufe zwei: Verwende komplexere Satzstrukturen, Fachbegriffe und beleuchte globale Zusammenhänge der Nachhaltigkeit.",
            _ => throw new ArgumentException("Invalid target group")
        };
        var lengthRequirement = story.TargetGroup switch
        {
            TargetGroup.PrimarySchool =>
                $"Jeder Geschichten Abschnitt soll mindestens {WordCountLimits.PrimarySchoolMin} und höchstens {WordCountLimits.PrimarySchoolMax} Wörter umfassen. Verwende einfache Sätze und kurze Absätze, damit der Text leicht verständlich bleibt.",
            TargetGroup.MiddleSchool =>
                $"Jeder Geschichten Abschnitt soll mindestens {WordCountLimits.MiddleSchoolMin} und höchstens {WordCountLimits.MiddleSchoolMax} Wörter umfassen. Nutze eine gut verständliche Sprache und veranschauliche deine Erklärungen mit Beispielen.",
            TargetGroup.HighSchool =>
                $"Jeder Geschichten Abschnitt soll mindestens {WordCountLimits.HighSchoolMin} und höchstens {WordCountLimits.HighSchoolMax} Wörter umfassen. Verwende detaillierte Beschreibungen, komplexere Satzstrukturen und vertiefende Erklärungen.",
            _ => throw new ArgumentException("Invalid target group")
        };
        var systemPrompt =
            "Du bist ein Geschichtenerzähler, der interaktive und textbasierte Geschichten zum Thema Nachhaltigkeit erstellt. Bitte beachte folgende Vorgaben:"
            + "[Thema]"
            + story.Topic
            + "[Zielgruppe]"
            + targetGroupString
            + "[Interaktivität]"
            + "Die Geschichte ist in mehrere Abschnitte unterteilt und enthält an jedem Entscheidungspunkt vier Optionen. Beachte:"
            + "- Jede Option hat einen Einflusswert zwischen -1 (starker negativer Einfluss) und 1 (starker positiver Einfluss)."
            + "- Die Summe der Einflusswerte der vier Optionen muss immer 0 ergeben."
            + "- Bei jedem Entscheidungspunkt präsentiere die vier Optionen und warte auf die Wahl der Teilnehmer."
            + "- Bei Erreichen des letzten Entscheidungspunkts, setze den Abschluss der Geschichte um."
            + "[Länge und Detailtiefe]"
            + lengthRequirement
            + "[Formatierung]"
            + "Antworte ausschließlich im folgenden JSON-Format:"
            + "{"
            + "  \"title\": \"Titel der Geschichte\","
            + "  \"intertitle\": \"Zwischentitel des Abschnitts\","
            + "  \"story\": \"Text des aktuellen Abschnitts.\","
            + "  \"options\": ["
            + "    { \"impact\": Wert zwischen -1 und 1, \"text\": \"Beschreibung der Option 1\" },"
            + "    { \"impact\": Wert zwischen -1 und 1, \"text\": \"Beschreibung der Option 2\" },"
            + "    { \"impact\": Wert zwischen -1 und 1, \"text\": \"Beschreibung der Option 3\" },"
            + "    { \"impact\": Wert zwischen -1 und 1, \"text\": \"Beschreibung der Option 4\" }"
            + "  ]"
            + "}"
            + "Stelle sicher, dass das JSON fehlerfrei geparst werden kann."
            + "[Kontext und Fortführung]"
            + "Berücksichtige alle bisherigen Entscheidungen und ihre Konsequenzen. Jeder Abschnitt sollte nahtlos an den vorherigen anschließen und die Geschichte weiterentwickeln.";
        var userPrompt =
            "Alle Teilnehmer sind bereit. Beginne bitte mit dem ersten Abschnitt deiner Geschichte zum Thema Nachhaltigkeit. "
            + $"Die Geschichte umfasst insgesamt {story.Length} Entscheidungspunkte."
            + "Bitte beachte:"
            + "- Verwende den vorgegebenen Sprachstil für die jeweilige Zielgruppe."
            + "- Erstelle den ersten Abschnitt inklusive eines Entscheidungspunkts, bei dem vier Optionen (mit jeweiligen Einflusswerten zwischen -1 und 1, deren Summe 0 ergeben muss) eingebaut werden."
            + "- Die Antwort muss exakt im folgenden JSON-Format erfolgen:"
            + "{"
            + "  \"title\": \"Titel der Geschichte\","
            + "  \"intertitle\": \"Zwischentitel des Abschnitts\","
            + "  \"story\": \"Text des aktuellen Abschnitts.\","
            + "  \"options\": ["
            + "    { \"impact\": Wert zwischen -1 und 1, \"text\": \"Beschreibung der Option 1\" },"
            + "    { \"impact\": Wert zwischen -1 und 1, \"text\": \"Beschreibung der Option 2\" },"
            + "    { \"impact\": Wert zwischen -1 und 1, \"text\": \"Beschreibung der Option 3\" },"
            + "    { \"impact\": Wert zwischen -1 und 1, \"text\": \"Beschreibung der Option 4\" }"
            + "  ]"
            + "}"
            + "Bitte beginne jetzt mit dem ersten Abschnitt.";

        List<ChatMessage> chatMessages =
        [
            new() { Role = ValidRoles.System, Content = systemPrompt },
            new() { Role = ValidRoles.User, Content = userPrompt }
        ];

        foreach (var part in story.Parts.Select((value, index) => new { value, index }))
        {
            StoryContent assistantContent = new()
            {
                Title = story.Title ?? throw new ArgumentNullException(nameof(story)),
                Intertitle = part.value.Intertitle,
                Story = part.value.Text,
                Options = part.value.Choices.Select(choice => new Option
                {
                    Impact = choice.Impact,
                    Text = choice.Text
                }).ToList()
            };
            chatMessages.Add(new ChatMessage
                { Role = ValidRoles.Assistant, Content = JsonSerializer.Serialize(assistantContent, JsonOptions) });

            if (!part.value.ChosenNumber.HasValue || part.value.ChosenNumber < 1 || part.value.ChosenNumber > 4)
                throw new ArgumentException($"Story part with id {part.value.Id} has invalid choice number");

            if (story.Length == part.index + 1)
            {
                userPrompt =
                    $"Die Option {part.value.ChosenNumber} \"{part.value.Choices.First(x => x.Number == part.value.ChosenNumber).Text}\" wurde gewählt. Du hast nun den letzten Entscheidungspunkt erreicht. Bitte schreibe den abschließenden Teil der Geschichte."
                    + "Achte darauf:"
                    + "- Führe die Geschichte konsequent zu einem runden Abschluss, indem du alle vorherigen Ereignisse berücksichtigst."
                    + "- Im letzten Abschnitt soll es keinen weiteren Entscheidungspunkt mehr geben. Daher müssen die Optionen im JSON-Array als leere, aber valide Einträge erscheinen (z. B. leere Strings)."
                    + "- Die Antwort muss weiterhin exakt im folgenden JSON-Format erfolgen:"
                    + "{"
                    + "  \"title\": \"Titel der Geschichte\","
                    + "  \"intertitle\": \"Zwischentitel des Schlussabschnitts\","
                    + "  \"story\": \"Abschließender Text der Geschichte, der alle Handlungsstränge zusammenführt.\","
                    + "  \"options\": ["
                    + "    { \"impact\": 0, \"text\": \"\" },"
                    + "    { \"impact\": 0, \"text\": \"\" },"
                    + "    { \"impact\": 0, \"text\": \"\" },"
                    + "    { \"impact\": 0, \"text\": \"\" }"
                    + "  ]"
                    + "}"
                    + "Bitte beende jetzt die Geschichte.";
                chatMessages.Add(new ChatMessage { Role = ValidRoles.User, Content = userPrompt });
            }
            else
            {
                userPrompt =
                    $"Die Option {part.value.ChosenNumber} \"{part.value.Choices.First(x => x.Number == part.value.ChosenNumber).Text}\"  wurde gewählt. Bitte fahre mit dem nächsten Abschnitt der Geschichte fort. Achte darauf:"
                    + "- Den bisherigen Kontext und die Konsequenzen der getroffenen Entscheidungen nahtlos einzubauen."
                    + "- Einen neuen Entscheidungspunkt zu integrieren, der wieder vier Optionen enthält (mit den Einflusswerten, deren Summe exakt 0 beträgt)."
                    + "- Den neuen Abschnitt im vorgegebenen JSON-Format auszugeben:"
                    + "{"
                    + "  \"title\": \"Titel der Geschichte\","
                    + "  \"intertitle\": \"Zwischentitel des neuen Abschnitts\","
                    + "  \"story\": \"Text des aktuellen Abschnitts, der auf den bisherigen Ereignissen aufbaut.\","
                    + "  \"options\": ["
                    + "    { \"impact\": Wert zwischen -1 und 1, \"text\": \"Beschreibung der Option 1\" },"
                    + "    { \"impact\": Wert zwischen -1 und 1, \"text\": \"Beschreibung der Option 2\" },"
                    + "    { \"impact\": Wert zwischen -1 und 1, \"text\": \"Beschreibung der Option 3\" },"
                    + "    { \"impact\": Wert zwischen -1 und 1, \"text\": \"Beschreibung der Option 4\" }"
                    + "  ]"
                    + "}"
                    + "Bitte setze die Geschichte an dieser Stelle fort.";
                chatMessages.Add(new ChatMessage { Role = ValidRoles.User, Content = userPrompt });
            }
        }

        return chatMessages;
    }

    // Benjamin Edlinger
    /// <summary>
    ///     Rebuilds the chat messages of the result for already rebuilt chat messages and the given story object
    /// </summary>
    /// <param name="story">The story object to rebuild the chat messages for</param>
    /// <param name="chatMessages">The already rebuilt chat messages based on the story</param>
    /// <param name="end">The end of the story</param>
    /// <returns>The rebuilt chat messages</returns>
    /// <exception cref="ArgumentNullException">If the story object is null, the chat messages are null or the end is null</exception>
    /// <exception cref="ArgumentException">If the chat messages list is empty</exception>
    private static List<ChatMessage> RebuildChatMessagesResult(Story story, List<ChatMessage> chatMessages,
        string end)
    {
        ArgumentNullException.ThrowIfNull(story);
        ArgumentNullException.ThrowIfNull(chatMessages);
        ArgumentNullException.ThrowIfNull(end);
        if (chatMessages.Count == 0) throw new ArgumentException("No messages to send");

        chatMessages.Add(new ChatMessage { Role = ValidRoles.Assistant, Content = end });
        var targetGroupString = story.TargetGroup switch
        {
            TargetGroup.PrimarySchool =>
                "Für Volksschüler (6-10 Jahre): Verwende einfache, bildhafte Sprache, kurze Sätze und anschauliche Beispiele, die aus dem Alltag der Kinder stammen.",
            TargetGroup.MiddleSchool =>
                "- Für Schüler der Sekundarstufe eins (11-14 Jahre): Nutze einen lebendigen, verständlichen Sprachstil und integriere altersgerechte Erklärungen und Beispiele. Baue moralische Konflikte ein, die für diesen Altersbereich nachvollziehbar sind.",
            TargetGroup.HighSchool =>
                "- Für Schüler der Sekundarstufe zwei (15-19 Jahre): Verwende einen anspruchsvolleren Sprachstil mit komplexeren Satzstrukturen und gegebenenfalls Fachbegriffen, um tiefere Zusammenhänge und globale Perspektiven zu beleuchten.",
            _ => throw new ArgumentException("Invalid target group")
        };
        var systemPrompt =
            "Du übernimmst die Rolle einer Lehrkraft, die gemeinsam mit den Teilnehmern die gerade durchlebte Geschichte zum Thema Nachhaltigkeit reflektiert."
            + "Bitte beachte dabei, dass du deinen Sprachstil, die Beispiele und die Diskussionsfragen an die jeweilige Zielgruppe anpasst:"
            + targetGroupString
            + "Deine Aufgabe besteht darin, den thematischen Kontext und die getroffenen Entscheidungen faktenbasiert und verständlich zu analysieren. Bitte folge diesen Schritten:"
            + "[Zusammenfassung und Analyse]"
            + "- Fasse die Geschichte in einem kurzen, prägnanten Fließtext zusammen. Stelle den Verlauf und die zentralen Ereignisse übersichtlich dar."
            + "- Analysiere, wie die Entscheidungen und Handlungen der Charaktere den Verlauf der Geschichte beeinflusst haben."
            + "[Positive und negative Entscheidungen]"
            + "- Erstelle eine Liste der positiven Entscheidungen, die in der Geschichte getroffen wurden. Erkläre zu jeder Entscheidung, warum sie sich positiv ausgewirkt hat und welche konkreten Vorteile daraus entstanden sind."
            + "- Erstelle eine Liste der negativen Entscheidungen, wenn negative Entscheidungen getroffen wurden, ansonsten ist die Liste leer. Beschreibe jeweils, welche negativen Konsequenzen daraus resultierten und wie sie den Verlauf der Geschichte beeinflusst haben."
            + "[Praktische Lehren]"
            + "- Ziehe konkrete Lehren aus der Geschichte und übertrage diese Erkenntnisse auf die reale Welt. Zeige auf, wie diese praktischen Erkenntnisse im Alltag oder in spezifischen Situationen angewendet werden können."
            + "[Diskussionsfragen]"
            + "- Formuliere gezielte Fragen, die zu einer offenen und respektvollen Diskussion anregen. Passe die Komplexität der Fragen an die Zielgruppe an, sodass sie zum Nachdenken anregen und unterschiedliche Perspektiven einbeziehen."
            + "Wichtig: Deine Analyse muss den nachhaltigen Kontext der Geschichte widerspiegeln und gleichzeitig sprachlich sowie inhaltlich auf die Zielgruppe abgestimmt sein."
            + "Bitte antworte ausschließlich im gültigen JSON-Format, damit deine Antwort korrekt dargestellt wird."
            + "Das erwartete JSON-Format lautet:"
            + "{"
            + "  \"summary\": \"Zusammenfassung und Analyse der Geschichte als Fließtext\","
            + "  \"positive_choices\": [\"Beschreibung der positiven Entscheidung 1\", \"Weitere positive Entscheidungen je nach Bedarf\"],"
            + "  \"negative_choices\": [\"Beschreibung der negativen Entscheidung 1\", \"Weitere negative Entscheidungen je nach Bedarf\"],"
            + "  \"learnings\": [\"Erkenntnis 1\", \"Weitere Erkenntnisse je nach Bedarf\"],"
            + "  \"discussion_questions\": [\"Frage 1\", \"Weitere Fragen je nach Bedarf\"]"
            + "}";
        chatMessages.Add(new ChatMessage { Role = ValidRoles.System, Content = systemPrompt });
        var userPrompt =
            "Die Geschichte ist soeben beendet. Du kannst nun die Analyse der durchlebten Geschichte erstellen. Denke daran, deinen Sprachstil, deine Beispiele und Diskussionsfragen an die Zielgruppe anzupassen. Bitte folge dabei genau den genannten Anweisungen und dem vorgegebenen JSON-Format.";
        chatMessages.Add(new ChatMessage { Role = ValidRoles.User, Content = userPrompt });

        if (story.Result == null) return chatMessages;

        AnalysisContent assistentContent = new()
        {
            Summary = story.Result.Summary,
            PositiveChoices = story.Result.PositiveChoices,
            NegativeChoices = story.Result.NegativeChoices,
            Learnings = story.Result.Learnings,
            DiscussionQuestions = story.Result.DiscussionQuestions
        };
        chatMessages.Add(new ChatMessage
            { Role = ValidRoles.Assistant, Content = JsonSerializer.Serialize(assistentContent, JsonOptions) });

        return chatMessages;
    }

    // Benjamin Edlinger
    /// <summary>
    ///     Deserializes the assistant content and returns the story part and the title of the story
    ///     If the word count of the story part is invalid, the assistant content will be fixed
    /// </summary>
    /// <param name="assistantContent">The generated assistant content to deserialize</param>
    /// <param name="targetGroup">The target group of the story</param>
    /// <param name="chatMessages">The previous chat messages</param>
    /// <returns>Returns the story part and the title of the story</returns>
    /// <exception cref="InvalidOperationException">Gets thrown if the assistant content or chat messages is null</exception>
    /// <exception cref="JsonException">Gets thrown if the assistant content could not be deserialized</exception>
    /// <exception cref="AIException">Gets thrown if the story part could not be fixed</exception>
    private async Task<(StoryPart, string)> GetStoryPart(string assistantContent, TargetGroup targetGroup,
        List<ChatMessage> chatMessages)
    {
        ArgumentNullException.ThrowIfNull(assistantContent);
        ArgumentNullException.ThrowIfNull(chatMessages);

        StoryContent messageContent;
        try
        {
            messageContent = JsonSerializer.Deserialize<StoryContent>(assistantContent) ??
                             throw new InvalidOperationException("Message content is null");
        }
        catch (JsonException e)
        {
            throw new JsonException("Failed to deserialize assistant content", e);
        }

        StoryPart storyPart = new()
        {
            Text = messageContent.Story,
            Intertitle = messageContent.Intertitle,
            Choices = messageContent.Options.Select((option, index) => new StoryChoice
            {
                Text = option.Text,
                Number = index + 1,
                Impact = option.Impact
            }).ToList()
        };

        var wordCount = GetWordCount(storyPart.Text);
        var prompt = targetGroup switch
        {
            TargetGroup.PrimarySchool when wordCount < WordCountLimits.PrimarySchoolMin * 0.8
                => $"Dieser Abschnitt ist zu kurz für die Volksschule. Bitte überarbeite ihn, damit er mindestens {WordCountLimits.PrimarySchoolMin} Wörter umfasst.",
            TargetGroup.PrimarySchool when wordCount > WordCountLimits.PrimarySchoolMax
                => $"Dieser Abschnitt ist zu lang für die Volksschule. Bitte überarbeite ihn, sodass er maximal {WordCountLimits.PrimarySchoolMax} Wörter umfasst.",
            TargetGroup.MiddleSchool when wordCount < WordCountLimits.MiddleSchoolMin * 0.7
                => $"Dieser Abschnitt ist zu kurz für die Sekundarstufe eins. Bitte überarbeite ihn, damit er mindestens {WordCountLimits.MiddleSchoolMin} Wörter umfasst.",
            TargetGroup.MiddleSchool when wordCount > WordCountLimits.MiddleSchoolMax
                => $"Dieser Abschnitt ist zu lang für die Sekundarstufe eins. Bitte überarbeite ihn, sodass er maximal {WordCountLimits.MiddleSchoolMax} Wörter umfasst.",
            TargetGroup.HighSchool when wordCount < WordCountLimits.HighSchoolMin * 0.6
                => $"Dieser Abschnitt ist zu kurz für die Sekundarstufe zwei. Bitte überarbeite ihn, damit er mindestens {WordCountLimits.HighSchoolMin} Wörter umfasst.",
            TargetGroup.HighSchool when wordCount > WordCountLimits.HighSchoolMax
                => $"Dieser Abschnitt ist zu lang für die Sekundarstufe zwei. Bitte überarbeite ihn, sodass er maximal {WordCountLimits.HighSchoolMax} Wörter umfasst.",
            _ => null
        };

        if (prompt == null) return (storyPart, messageContent.Title);

        _logger.LogWarning("Story part with id {Id} for {TargetGroup} has invalid word count: {WordCount}",
            storyPart.Id, targetGroup, wordCount);
        chatMessages.Add(new ChatMessage
            { Role = ValidRoles.Assistant, Content = JsonSerializer.Serialize(storyPart, JsonOptions) });
        chatMessages.Add(new ChatMessage { Role = ValidRoles.User, Content = prompt });

        try
        {
            var fixedContent = await FetchAssistantContent(chatMessages, 0.7f, 0.7f);
            messageContent = JsonSerializer.Deserialize<StoryContent>(fixedContent) ??
                             throw new InvalidOperationException("Message content is null");
            storyPart.Text = messageContent.Story;
            storyPart.Intertitle = messageContent.Intertitle;
            storyPart.Choices = messageContent.Options.Select((option, index) => new StoryChoice
            {
                Text = option.Text,
                Number = index + 1,
                Impact = option.Impact
            }).ToList();
            return (storyPart, messageContent.Title);
        }
        catch (Exception e)
        {
            _logger.LogError("Failed to fix story part with id {Id}: {Exception}", storyPart.Id, e);
            throw new AIException("Failed to fix story part", e);
        }
    }

    // Benjamin Edlinger
    /// <summary>
    ///     Counts the number of words in the given text
    /// </summary>
    /// <param name="text">The text to count the words for</param>
    /// <returns>Count of words in the text</returns>
    private static int GetWordCount(string text)
    {
        char[] delimiters = [' ', '\r', '\n'];
        return text.Split(delimiters, StringSplitOptions.RemoveEmptyEntries).Length;
    }

    // Benjamin Edlinger
    /// <summary>
    ///     Gets the story result from the given assistant content
    /// </summary>
    /// <param name="assistantContent">The assistant content to get the story result from</param>
    /// <param name="end">The end of the story</param>
    /// <returns>The story result</returns>
    /// <exception cref="InvalidOperationException">If the message content is null</exception>
    /// <exception cref="JsonException">If the assistant content could not be deserialized</exception>
    private static StoryResult GetStoryResult(string assistantContent, string end)
    {
        ArgumentNullException.ThrowIfNull(assistantContent);
        ArgumentNullException.ThrowIfNull(end);

        AnalysisContent messageContent;
        try
        {
            messageContent = JsonSerializer.Deserialize<AnalysisContent>(assistantContent) ??
                             throw new InvalidOperationException("Message content is null");
        }
        catch (JsonException e)
        {
            throw new JsonException("Failed to deserialize assistant content", e);
        }

        return new StoryResult
        {
            Text = end,
            Summary = messageContent.Summary,
            PositiveChoices = messageContent.PositiveChoices,
            NegativeChoices = messageContent.NegativeChoices,
            Learnings = messageContent.Learnings,
            DiscussionQuestions = messageContent.DiscussionQuestions
        };
    }

    // Benjamin Edlinger
    /// <summary>
    ///     Fetches the assistant content based on the given chat messages, temperature and topP
    /// </summary>
    /// <param name="chatMessages">The chat messages to fetch the assistant content for</param>
    /// <param name="temperature">The temperature for the assistant content</param>
    /// <param name="topP">The topP for the assistant content</param>
    /// <param name="isJsonResponse">If the response should be a json object</param>
    /// <returns>The assistant content</returns>
    /// <exception cref="ArgumentException">If the chat messages are empty, the temperature is invalid or the topP is invalid</exception>
    /// <exception cref="HttpRequestException">If the request failed</exception>
    /// <exception cref="InvalidOperationException">If the response object is null or the assistant content is null or empty</exception>
    /// <exception cref="JsonException">If the response content could not be deserialized</exception>
    private async Task<string> FetchAssistantContent(List<ChatMessage> chatMessages, float temperature, float topP,
        bool isJsonResponse = true)
    {
        ArgumentNullException.ThrowIfNull(_client);
        ArgumentNullException.ThrowIfNull(chatMessages);
        if (chatMessages.Count == 0) throw new ArgumentException("No messages to send");
        if (temperature < 0 || temperature > 1) throw new ArgumentException("Invalid temperature");
        if (topP < 0 || topP > 1) throw new ArgumentException("Invalid topP");

        object requestBody;
        if (isJsonResponse)
            requestBody = new
            {
                model = TextGenerationModel,
                messages = chatMessages,
                temperature,
                top_p = topP,
                response_format = new { type = "json_object" }
            };
        else
            requestBody = new
            {
                model = ImagePromptModel,
                messages = chatMessages,
                temperature,
                top_p = topP
            };

        HttpRequestMessage request = new(HttpMethod.Post, "/v1/openai/chat/completions")
        {
            Content = new StringContent(JsonSerializer.Serialize(requestBody, JsonOptions), Encoding.UTF8,
                "application/json")
        };

        HttpResponseMessage response = null!;
        string responseString;
        try
        {
            response = await _client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var byteArray = await response.Content.ReadAsByteArrayAsync();
            responseString = Encoding.UTF8.GetString(byteArray, 0, byteArray.Length);
            _logger.LogInformation("Received response from server: {Response}", responseString);
        }
        catch (HttpRequestException e)
        {
            throw new HttpRequestException($"Request failed with status code {response.StatusCode}", e);
        }

        try
        {
            var responseObject = JsonSerializer.Deserialize<Response>(responseString) ??
                                 throw new InvalidOperationException("Response object is null");
            _logger.LogDebug("Assistant response: {Response}", responseObject.Choices[0].Message.Content);
            return responseObject.Choices[0].Message.Content ??
                   throw new InvalidOperationException("Assistant content is null or empty");
        }
        catch (JsonException e)
        {
            throw new JsonException("Failed to deserialize response content", e);
        }
    }

    /// <summary>
    ///     Kacper Bohaczyk
    /// </summary>
    /// <param name="story"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="ArgumentNullException"></exception>
    private List<ChatMessage> RebuildChatMessagesQuiz(Story story, QuizRequest config,
        List<ChatMessage> chatMessages)
    {
        ArgumentNullException.ThrowIfNull(story);

        var targetGroupString = story.TargetGroup switch
        {
            TargetGroup.PrimarySchool =>
                "Die Teilnehmer, welche den Quiz  durchführen, sind Volksschüler im Alter von 6 bis 10 Jahren. Pass deinen Stil an diese Zielgruppe an und verwende einfache Sprache mit kurzen und klaren Sätzen.",
            TargetGroup.MiddleSchool =>
                "Die Teilnehmer, welche den Quiz  durchführen, sind Schüler der Sekundarstufe eins im Alter von 11 bis 14 Jahren. Pass deinen Stil an diese Zielgruppe und verwende einen passend anspruchsvollen Wortschatz und Satzbau.",
            TargetGroup.HighSchool =>
                "Die Teilnehmer, welche den Quiz  durchführen, sind Schüler der Sekundarstufe zwei im Alter von 15 bis 19 Jahren. Pass deinen Stil an diese Zielgruppe an und verwende eine anspruchsvollere Sprache mit komplexeren Satzstrukturen und Fachbegriffen.",
            _ => throw new ArgumentException("Invalid target group")
        };
        // Der Teil drüber soll zusätylich in den SystemPromt reinkommen


        try
        {
            var systemPrompt =
                $"Von jetzt an versetzt du dich in die Rolle eines proffesionellem Quizersteller mit besonderer Expertise in dem Bereich Nachhaltigkeit im genauen bei {story.Topic}." +
                "Deine Aufgabe ist, ein Quiz zu erstellen, welcher auf der zuvor generierten Geschichte basiert." +
                "Die Fragen sollen sich ausschließlich auf die in der Geschichte thematisierten Nachhaltigkeitsaspekte konzentrieren, und im genauerem dem ausgewählten Pfad vom User folgen. " +
                "Wichtig ist es das du den ganzen Quiz, das beduetet die Fragen und die Antorten in  einer Response ausgibst. " +
                $"Formatierungsrichtlinien: {targetGroupString} " +
                "{'Title': 'Der Titel des ganzen Quizes'," +
                "'NumberQuestions': 'Anzahl an Questions'," +
                "'Questions': {'Text': 'Der Titel der jeweiligen Frage'," +
                //"'IsMultipleResponse': 'Besagt ob die Frage eine MultipleRespose Frage ist - bei ja wird True gesetzt bei nein False '" + 
                "'Number': 'Die Nummer der Frage', " +
                "'Choices': [{'Number': 'Die Nummer der Auswahlmöglichkeit'," +
                "'Text': 'Der Text zur jeweiligen Auswahlmöglichkeit', " +
                "'IsCorrect': 'Ein Wahrheitswert, der angibt, ob die Auswahl korrekt ist'}]}} " +
                "Das Quiz soll aus ausschließlich aus." +
                string.Join(", ", config.Types.Select(t =>
                {
                    return t switch
                    {
                        QuizType.MultipleResponse =>
                            "Multiple Response - bedeutet, dass es mehrere Antwortmöglichkeiten gibt. Hierbei müssen meherere Richtig sein. überprüfe das von 2-4 Auswahlmöglichkeiten  mindestens 2 richtig",
                        QuizType.SingleResponse =>
                            "Single response - bedeutet,  dass es mehrere Antwortmöglichkeiten gibt. Hierbei ist muss nur eine der 4 Richtig sein ",
                        QuizType.TrueFalse =>
                            "True/False - bedeutet, dass es 2 Antwortmöglichkeiten gibt(Wahr und Falsch). Eines der beiden kann hierbei nur Richitg sein ",
                        _ => throw new ArgumentException("Invalid quiz type")
                    };
                })) +
                $"Fragen bestehen und soll {config.NumberQuestions} Frage/n lang sein.";
            ;
            var userPrompt =
                $"Alle Fragen sollen zum Gebiet {story.Topic} passen. Generiere das Quiz auf Basis der durchlebten Story.";

            chatMessages.Add(new ChatMessage { Role = ValidRoles.System, Content = systemPrompt });
            chatMessages.Add(new ChatMessage { Role = ValidRoles.User, Content = userPrompt });

            _logger.LogDebug(systemPrompt);
            _logger.LogDebug(chatMessages.ToString());
        }
        catch (Exception e)
        {
            throw new AIException("Failed to generate Quiz", e);
        }

        return chatMessages;
    }


    public static string GetEnumMemberValue(Enum enumValue)
    {
        var type = enumValue.GetType();
        var memberInfo = type.GetMember(enumValue.ToString()).FirstOrDefault();

        if (memberInfo != null)
        {
            var attribute = memberInfo.GetCustomAttribute<EnumMemberAttribute>();
            if (attribute != null) return attribute.Value; // Return the EnumMember Value
        }

        return enumValue.ToString(); // Fallback to enum name if no attribute is found
    }


    /// <summary>
    ///     Kacper Bohaczyk
    /// </summary>
    /// <param name="assistantContent"></param>
    /// <returns></returns>
    private static Quiz GetQuiz(string assistantContent)
    {
        ArgumentNullException.ThrowIfNull(assistantContent);

        QuizContent messageContent;
        try
        {
            messageContent = JsonSerializer.Deserialize<QuizContent>(assistantContent) ??
                             throw new InvalidOperationException("Message content is null");
        }
        catch (JsonException e)
        {
            throw new JsonException("Failed to deserialize assistant content", e);
        }


        return new Quiz
        {
            Title = messageContent.Title,
            NumberQuestions = messageContent.NumberQuestions,
            Questions = messageContent.Questions.Select((question, index) => new QuizQuestion
            {
                Number = question.Number,
                Text = question.Text,
                IsMultipleResponse = question.Choices.Where(c => c.IsCorrect).Count() > 1,

                Choices = question.Choices.Select((choice, index) => new QuizChoice
                {
                    Number = choice.Number,
                    Text = choice.Text,
                    IsCorrect = choice.IsCorrect
                }).ToList()
            }).ToList()
        };
    }
}

// Benjamin Edlinger
/// <summary>
///     Exception for AI service
/// </summary>
public class AIException : Exception
{
    public AIException()
    {
    }

    public AIException(string message)
        : base(message)
    {
    }

    public AIException(string message, Exception inner)
        : base(message, inner)
    {
    }
}

// Benjamin Edlinger
public static class WordCountLimits
{
    public const int PrimarySchoolMin = 70;
    public const int PrimarySchoolMax = 90;
    public const int MiddleSchoolMin = 120;
    public const int MiddleSchoolMax = 140;
    public const int HighSchoolMin = 170;
    public const int HighSchoolMax = 190;
}

// Benjamin Edlinger
public static class ValidRoles
{
    public const string System = "system";
    public const string User = "user";
    public const string Assistant = "assistant";
}

// Benjamin Edlinger
public class ChatMessage
{
    private string _role = null!;

    [JsonPropertyName("role")]
    public string Role
    {
        get => _role;
        set
        {
            if (value != ValidRoles.System && value != ValidRoles.User && value != ValidRoles.Assistant)
                throw new ArgumentException("Invalid role");

            _role = value;
        }
    }

    [JsonPropertyName("content")] public string Content { get; set; } = null!;
}

// Benjamin Edlinger
public class Response
{
    [JsonPropertyName("id")] public string Id { get; set; } = null!;

    [JsonPropertyName("object")] public string Object { get; set; } = null!;

    [JsonPropertyName("created")] public long Created { get; set; }

    [JsonPropertyName("model")] public string Model { get; set; } = null!;

    [JsonPropertyName("choices")] public List<Choice> Choices { get; set; } = null!;

    [JsonPropertyName("usage")] public Usage Usage { get; set; } = null!;
}

// Benjamin Edlinger
public class Choice
{
    [JsonPropertyName("index")] public int Index { get; set; }

    [JsonPropertyName("message")] public Message Message { get; set; } = null!;

    [JsonPropertyName("finish_reason")] public string FinishReason { get; set; } = null!;
}

// Benjamin Edlinger
public class Message
{
    private string _role = null!;

    [JsonPropertyName("role")]
    public string Role
    {
        get => _role;
        set
        {
            if (value != ValidRoles.System && value != ValidRoles.User && value != ValidRoles.Assistant)
                throw new ArgumentException("Invalid role");

            _role = value;
        }
    }

    [JsonPropertyName("content")] public string Content { get; set; } = null!;
}

// Benjamin Edlinger
public class StoryContent
{
    [JsonPropertyName("title")] public string Title { get; set; } = null!;

    [JsonPropertyName("intertitle")] public string Intertitle { get; set; } = null!;

    [JsonPropertyName("story")] public string Story { get; set; } = null!;

    [JsonPropertyName("options")] public List<Option> Options { get; set; } = null!;
}

// Benjamin Edlinger
public class Option
{
    [JsonPropertyName("impact")] public float Impact { get; set; }

    [JsonPropertyName("text")] public string Text { get; set; } = null!;
}

// Benjamin Edlinger
public class AnalysisContent
{
    [JsonPropertyName("summary")] public string Summary { get; set; } = null!;

    [JsonPropertyName("positive_choices")] public string[] PositiveChoices { get; set; } = null!;

    [JsonPropertyName("negative_choices")] public string[] NegativeChoices { get; set; } = null!;

    [JsonPropertyName("learnings")] public string[] Learnings { get; set; } = null!;

    [JsonPropertyName("discussion_questions")]
    public string[] DiscussionQuestions { get; set; } = null!;
}

// Benjamin Edlinger
public class ImageContent
{
    [JsonPropertyName("request_id")] public string RequestId { get; set; } = null!;

    [JsonPropertyName("inference_status")] public InferenceStatus InferenceStatus { get; set; } = null!;

    [JsonPropertyName("images")] public List<string> Images { get; set; } = null!;

    [JsonPropertyName("nsfw_content_detected")]
    public List<bool> NsfwContentDetected { get; set; } = null!;

    [JsonPropertyName("seed")] public long Seed { get; set; }
}

// Benjamin Edlinger
public class InferenceStatus
{
    [JsonPropertyName("status")] public string Status { get; set; } = null!;

    [JsonPropertyName("runtime_ms")] public int RuntimeMs { get; set; }

    [JsonPropertyName("cost")] public double Cost { get; set; }

    [JsonPropertyName("tokens_generated")] public int? TokensGenerated { get; set; }

    [JsonPropertyName("tokens_input")] public int? TokensInput { get; set; }
}

// Benjamin Edlinger
public class Usage
{
    [JsonPropertyName("prompt_tokens")] public int PromptTokens { get; set; }

    [JsonPropertyName("completion_tokens")]
    public int CompletionTokens { get; set; }

    [JsonPropertyName("total_tokens")] public int TotalTokens { get; set; }

    [JsonPropertyName("estimated_cost")] public double EstimatedCost { get; set; }
}

// Kacper Bohaczyk
public class QuizContent
{
    [JsonPropertyName("Title")] public string Title { get; set; } = null!;

    [JsonPropertyName("NumberQuestions")] public uint NumberQuestions { get; set; }

    [JsonPropertyName("Questions")] public List<Questions> Questions { get; set; } = null!;
}

public class Questions
{
    [JsonPropertyName("Text")] public string Text { get; set; } = null!;

    [JsonPropertyName("Number")] public int Number { get; set; }

    [JsonPropertyName("IsMultipleResponse")]
    public bool IsMultipleResponse { get; set; }

    [JsonPropertyName("Choices")] public List<Choices> Choices { get; set; } = null!;
}

public class Choices
{
    [JsonPropertyName("Text")] public string Text { get; set; } = null!;

    [JsonPropertyName("Number")] public int Number { get; set; }

    [JsonPropertyName("IsCorrect")] public bool IsCorrect { get; set; }
}