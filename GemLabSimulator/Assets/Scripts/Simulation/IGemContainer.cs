public interface IGemContainer
{
    bool TryAddGem(GemInstance gem);
    void RemoveGem(GemInstance gem);
}
