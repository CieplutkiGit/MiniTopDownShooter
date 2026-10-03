using System;

namespace Application.Economy
{
    /// <summary>
    /// Pure C# provider for the default economy policy in the Application layer.
    /// Allows the Unity layer to inject the production policy without introducing Unity dependencies.
    /// </summary>
    public static class EconomyPolicyProvider
    {
        private static Func<IEconomyPolicy> s_defaultPolicyProvider;

        public static IEconomyPolicy DefaultPolicy => s_defaultPolicyProvider?.Invoke();

        public static void SetDefaultPolicyProvider(Func<IEconomyPolicy> provider)
        {
            s_defaultPolicyProvider = provider;
        }

        public static void ResetForTesting()
        {
            s_defaultPolicyProvider = null;
        }
    }
}
