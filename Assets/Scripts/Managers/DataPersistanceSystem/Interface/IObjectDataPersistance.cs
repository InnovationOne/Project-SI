using Unity.Collections;

public interface IObjectDataPersistence {
    public void LoadObject(FixedString4096Bytes data);
    public string SaveObject();

}
