using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace SaveManager {
    public static class KeyManager {
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
        
        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
        private static IntPtr hookID = IntPtr.Zero;
        private static LowLevelKeyboardProc hookProc;
        private static Action callback;
        private static Key targetKey = Key.None;
        
        public static void RegisterKey(Window owner, string keyString, Action callbackAction) {
            callback = callbackAction;
            
            try {
                targetKey = (Key)Enum.Parse(typeof(Key), keyString);
            } catch {
                MessageBox.Show(
                    $"Invalid key: {keyString}", 
                    "Error", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Error
                );
                return;
            }
            
            hookProc = new LowLevelKeyboardProc(HookCallback);
            using (Process currentProcess = Process.GetCurrentProcess())
            using (ProcessModule currentModule = currentProcess.MainModule) {
                hookID = SetWindowsHookEx(WH_KEYBOARD_LL, hookProc, 
                    GetModuleHandle(currentModule.ModuleName), 0);
            }
            
            if (hookID == IntPtr.Zero) {
                int error = Marshal.GetLastWin32Error();
                MessageBox.Show(
                    $"Could not register keyboard hook. Error code: {error}", 
                    "Error", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Error
                );
            }
        }
        
        public static void UnregisterKey(Window owner) {
            if (hookID != IntPtr.Zero) {
                UnhookWindowsHookEx(hookID);
                hookID = IntPtr.Zero;
                targetKey = Key.None;
            }
        }
        
        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam) {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN) {
                int vkCode = Marshal.ReadInt32(lParam);
                Key key = KeyInterop.KeyFromVirtualKey(vkCode);
                
                if (key == targetKey) {
                    Application.Current.Dispatcher.Invoke(() => {
                        callback?.Invoke();
                    });
                }
            }

            return CallNextHookEx(hookID, nCode, wParam, lParam);
        }
    }
}