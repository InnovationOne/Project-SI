public interface IBuildable {
    void StartConstruction();
    void FinishConstruction();
    void Upgrade();
    bool IsUnderConstruction();
}
