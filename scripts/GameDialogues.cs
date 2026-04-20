using System.Collections.Generic;

/// All in-game dialogue content in one place.
/// Every script that needs dialogue lines calls these static methods.
public static class GameDialogues
{
    // ─── Orb intro (hallway, first visit) ────────────────────────────────────

    public static List<DialogueLine> OrbIntro() => new()
    {
        new("Orb",    "This house feels strange... maybe we should check the kitchen first."),
        new("Player", "You always say that."),
        new("Orb",    "And I'm usually right."),
        new("Orb",    "Also — is that an apple on the table? Might be useful."),
    };

    // ─── Mother NPC ───────────────────────────────────────────────────────────

    public static List<DialogueLine> MotherGiveQuest() => new()
    {
        new("Mother", "Oh, there you are. I've been looking for you."),
        new("Mother", "Could you bring me the apple from the hallway table?"),
        new("Mother", "I'd get it myself, but my back has been acting up today..."),
        new("Player", "Sure, I'll get it for you."),
        new("Orb",    "An apple? Simple enough. It should be on that table back in the hallway."),
    };

    public static List<DialogueLine> MotherNoItemYet() => new()
    {
        new("Mother", "Did you find the apple?"),
        new("Player", "Not yet. I'll keep looking."),
        new("Orb",    "It's on the table in the hallway. Hard to miss, actually."),
    };

    public static List<DialogueLine> MotherCompleteQuest() => new()
    {
        new("Mother", "Oh, you found it! Thank you so much."),
        new("Player", "Happy to help."),
        new("Mother", "You're a good child. Now go rest — the house has been... strange lately."),
        new("Orb",    "Strange. Yes. That's one word for it."),
    };

    public static List<DialogueLine> MotherThanksAgain() => new()
    {
        new("Mother", "Thank you again, dear. That apple was just what I needed."),
    };
}
