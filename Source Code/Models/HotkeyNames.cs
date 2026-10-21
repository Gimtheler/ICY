namespace ICY.Models;

public static class HotkeyNames
{
    public static string Describe(int vk)
    {
        return vk switch
        {
            0 => "None",
            1 => "Mouse left",
            2 => "Mouse right",
            4 => "Mouse middle",
            5 => "Mouse 4",
            6 => "Mouse 5",
            8 => "Backspace",
            9 => "Tab",
            13 => "Enter",
            16 => "Shift",
            17 => "Ctrl",
            18 => "Alt",
            20 => "Caps Lock",
            32 => "Space",
            45 => "Insert",
            46 => "Delete",
            >= 0x30 and <= 0x39 => ((char)vk).ToString(),
            >= 0x41 and <= 0x5A => ((char)vk).ToString(),
            >= 0x70 and <= 0x7B => "F" + (vk - 0x6F),
            _ => "VK " + vk
        };
    }
}
