// This script is for objects to implement saving and loading since they differ from crops or items
// where all attibutes are the same
public interface IObjectDataPersistence {
    public void LoadObject(string data);
    public string SaveObject();

}
