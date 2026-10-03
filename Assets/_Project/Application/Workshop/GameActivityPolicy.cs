namespace Application.Workshop
{
    public static class GameActivityPolicy
    {
        public static bool CanMove(GameState state)
        {
            return state == GameState.Playing ||
                   state == GameState.WorkshopRoaming ||
                   state == GameState.WorkshopFiringRange;
        }

        public static bool CanFire(GameState state)
        {
            return state == GameState.Playing ||
                   state == GameState.WorkshopFiringRange;
        }

        public static bool CanSwitchWeapon(GameState state)
        {
            return state == GameState.Playing ||
                   state == GameState.WorkshopRoaming ||
                   state == GameState.WorkshopFiringRange;
        }

        public static bool CanSelectWeapon(GameState state)
        {
            return CanSwitchWeapon(state);
        }

        public static bool AdvancesRun(GameState state)
        {
            return state == GameState.Playing;
        }

        public static bool IsTimeFrozen(GameState state)
        {
            return state == GameState.Menu ||
                   state == GameState.Paused ||
                   state == GameState.GameOver ||
                   state == GameState.Victory ||
                   state == GameState.WorkshopEditing ||
                   state == GameState.Loading ||
                   state == GameState.DeploymentBriefing;
        }
    }
}
