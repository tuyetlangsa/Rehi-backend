using Rehi.Domain.Common;

namespace Rehi.Domain.Flashcards;

public static class FlashcardErrors 
{
    public static Error NotFound => new("Flashcard.NotFound", "Flashcard is not found", ErrorType.NotFound);
    public static Error AlreadyExisted => new("Flashcard.AlreadyExisted", "Flashcard is already existed",ErrorType.Conflict);
}