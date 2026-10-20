public interface IGemContainer
{
    bool TryAddGem(GemInstance gem);
    void RemoveGem(GemInstance gem);

    // Lets a container refuse a stone with an explanation (the chest keeps identified stones, the cabinet donations...).
    bool CanAccept(GemInstance gem, out string reason)
    {
        reason = null;
        return true;
    }
}
