
namespace StorytellersTable.Campaign.Modes
{
    /// <summary>
    /// Defining lifecycle hooks for a modular campaign state
    /// </summary>
    public interface ICampaignMode
    {
        /// <summary>
        /// Called when the state is transitioned into. Handles initiallization: UI loading, input context setup, etc.
        /// </summary>
        public void Enter();

        /// <summary>
        /// Should be called every frame from the driver class. Processes active state logic and mode inputs.
        /// </summary>
        public void UpdateMode();

        /// <summary>
        /// Called when transitioning out of this state. Handles clean up, resourse disposal, UI destruction, etc.
        /// </summary>
        public void Exit();
    }
}
