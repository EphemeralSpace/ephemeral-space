using System.Numerics;
using Robust.Shared.GameStates;

namespace Content.Shared._ES.Viewcone;

/// <summary>
/// ⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⡠⠀⠀⠀⡀⢄⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
/// ⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⢰⣴⡿⣟⣿⣻⣦⠆⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
/// ⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⣲⣯⢿⡽⣞⣷⣻⡞⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
/// ⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⡈⢿⣽⣯⢿⡽⣞⣷⡻⢥⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
/// ⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⢠⠉⠀⠀⠀⠀⠀⠀⠀⠀⠀⠄⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
/// ⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠆⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠰⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
/// ⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠐⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⢃⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
/// ⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⢀⣣⣀⣀⡀⠀⠀⠀⠀⠀⠀⢀⣀⣀⣬⡄⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
/// ⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⣌⣿⡽⣯⣟⣿⣻⢟⣿⡻⣟⣯⢿⡽⣯⡡⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
/// ⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠰⣽⣷⣻⢷⣻⢾⣽⣻⢾⣽⣻⣞⣯⣟⡷⣧⡆⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
/// ⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⢳⣟⡾⣽⢯⡿⣽⢾⣽⣻⣞⣷⣻⢾⣭⣟⡷⡞⡀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
/// ⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⣘⠉⠙⠙⠯⠿⢽⣯⣟⣾⣳⣟⣾⡽⠻⠾⠙⠋⠉⢁⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
/// ⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⢠⡀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠆⠀⠀⠀⠀⠀⠀⠀⠀⠀
/// ⠀⠀⠀⠀⢀⣤⣤⡶⣶⣶⡆⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⢰⣶⣶⢶⣤⣠⡀⠀⠀⠀
/// ⠀⠀⠀⣰⣟⡷⣯⣟⣷⡻⣀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⣀⢷⣯⣟⡾⣽⣳⣆⠀⠀
/// ⠀⠀⢀⡿⣾⣽⣳⣟⡾⢳⣟⣿⣲⢦⣤⣄⣀⣀⠀⠀⠀⠀⠀⠀⢀⣀⣀⣤⣴⢶⡿⣽⡛⣾⣽⢻⣷⣻⢾⡄⠀
/// ⠀⠀⢸⣟⡷⣯⢷⣯⣏⡿⣽⢾⣽⣻⢾⡽⣯⣟⡿⣻⣟⢿⣻⣟⡿⣯⣟⡷⣯⢿⣽⣳⢿⣹⣞⡿⡾⣽⣻⣄⠀
/// ⠀⠀⣿⢾⡽⣯⣟⡾⣼⡽⣯⣟⡾⣽⢯⣟⡷⣯⣟⡷⣯⣟⡷⣯⣟⡷⣯⢿⣽⣻⢾⡽⣯⢧⢯⡿⣽⣳⣟⣎⠀
/// ⠀⢰⣯⢿⣽⣳⢯⡷⣯⣟⡷⣯⢿⣽⣻⢾⣽⣳⢯⣟⡷⣯⣟⡷⣯⢿⣽⣻⢾⡽⣯⢿⣽⣻⢾⣽⣳⣟⡾⣽⠇
/// ⠀⣼⣞⡿⡾⣽⢯⣟⡷⣯⢿⣽⣻⢾⣽⣻⢾⣽⣻⢾⣽⣳⢯⡿⣽⣻⢾⡽⣯⢿⣽⣻⢾⣽⣻⣞⡷⣯⢿⣽⣣
/// ⢠⡾⣽⣻⡽⣯⣟⡾⣽⢯⣟⡾⣽⣻⢾⣽⣻⢾⣽⣻⢾⡽⣯⣟⡷⣯⢿⡽⣯⣟⡾⣽⣻⣞⡷⣯⢿⣽⣻⣞⣷
/// ⠀⠻⣷⣯⣟⡷⣯⢿⣽⣻⢾⣽⣳⢯⣟⡾⣽⣻⢾⡽⣯⣟⡷⣯⢿⡽⣯⣟⡷⣯⣟⣷⣻⢾⡽⣯⣟⣾⣳⢿⡞
/// ⠀⠀⠈⠓⠛⠙⠋⠛⠚⠙⠛⠚⠋⠛⠚⠛⠓⠛⠋⠛⠓⠋⠛⠙⠋⠛⠓⠋⠛⠓⠛⠚⠙⠋⠛⠓⠛⠚⠛⠉⠀
///           THE CONE MAN APPROACHES
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ESViewconeComponent : Component
{
    [DataField, AutoNetworkedField]
    public float ConeAngle = 225f;

    [DataField, AutoNetworkedField]
    public float ConeFeather = 3f;

    [DataField, AutoNetworkedField]
    public float ConeIgnoreRadius = 0.65f;

    [DataField, AutoNetworkedField]
    public float ConeIgnoreFeather = 0.03f;

    // Clientside, used for lerping view angle
    // and keeping it consistent across all overlays
    public Angle ViewAngle;
    public Angle? DesiredViewAngle = null;
    public Angle LastMouseRotationAngle;
    public Vector2 LastWorldPos;
    public Angle LastWorldRotationAngle;
}
