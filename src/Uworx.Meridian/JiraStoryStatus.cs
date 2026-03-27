namespace Uworx.Meridian;

public record JiraStoryStatus(
    string Key,
    string Title,
    string Status,
    int StoryPoints
);
