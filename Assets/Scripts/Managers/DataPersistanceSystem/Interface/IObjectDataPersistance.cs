using Unity.Collections;

public interface IObjectDataPersistence {
    public void LoadObject(string data);
    public string SaveObject();

}
