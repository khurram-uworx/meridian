using System.Collections.Generic;

namespace Uworx.Meridian.CourseSource;

public record QuizQuestion(string Text, List<string> Options, int CorrectIndex);
