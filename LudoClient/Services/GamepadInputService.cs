using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LudoClient.Services
{
    // Shared: GamepadInputService.cs
    public class GamepadInputService : IGamepadInputService
    {
        public event Action<string, string, bool>? ButtonChanged;
        public event Action<string, string, float>? AxisChanged;

        public void OnButtonChanged(string deviceName, string button, bool isDown)
            => ButtonChanged?.Invoke(deviceName, button, isDown);

        public void OnAxisChanged(string deviceName, string axis, float value)
            => AxisChanged?.Invoke(deviceName, axis, value);
    }
    // Shared: IGamepadInputService.cs
    public interface IGamepadInputService
    {
        // ✅ expose events on the interface
        event Action<string, string, bool> ButtonChanged;
        event Action<string, string, float> AxisChanged;

        // publisher methods (called by MainActivity)
        void OnButtonChanged(string deviceName, string button, bool isDown);
        void OnAxisChanged(string deviceName, string axis, float value);
    }
}
