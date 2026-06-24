/// <summary>
/// Optional hook for objects dragged by TouchDragger.
/// TouchDragger calls these when it picks up / releases an object that
/// implements this interface. Lets a draggable react (e.g. snap into place)
/// without TouchDragger having to know anything about the puzzle.
/// </summary>
public interface IDragNotify
{
    void OnDragPickedUp();
    void OnDragReleased();
}
