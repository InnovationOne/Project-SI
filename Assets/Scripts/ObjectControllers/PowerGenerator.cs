public class PowerGenerator : ItemProducer {

    /// <summary>
    /// Initializes and subscribes to time-based events.
    /// </summary>
    private void Start() => TimeAndWeatherManager.Instance.OnNextDayStarted += ItemProducerProcess;

    /// <summary>
    /// Processes the conversion of items based on the recipe timer.
    /// </summary>
    private void ItemProducerProcess() {
        switch ()
    }
}
