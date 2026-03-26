// ClientTypes.cs - Stub types for the XUI / client-side 7DTD API.
//
// These types mirror the public surface of the XUI system and LocalPlayerUI
// closely enough for CI compilation without an installed 7DTD client.
// At runtime the game loads its own assemblies, which shadow these stubs.

using System.Collections.Generic;

// ---------------------------------------------------------------------------
// Logging
// ---------------------------------------------------------------------------

/// <summary>
/// Minimal logging stub — delegates to SdtdConsole when available.
/// At runtime this is shadowed by the real Log class from Assembly-CSharp.dll.
/// </summary>
public static class Log
{
    public static void Out(string message)     => SdtdConsole.Instance?.Output(message);
    public static void Warning(string message) => SdtdConsole.Instance?.Output("[WARN] " + message);
    public static void Error(string message)   => SdtdConsole.Instance?.Output("[ERROR] " + message);
}

// ---------------------------------------------------------------------------
// XUI controller base
// ---------------------------------------------------------------------------

/// <summary>
/// Base class for all XUI window controllers. Subclasses override
/// GetBindingValue to feed data to XUI XML elements via the binding="..." attribute.
/// </summary>
public class XUiController
{
    /// <summary>Called once when the controller is first initialised.</summary>
    public virtual void Init() { }

    /// <summary>Called each time the window is opened.</summary>
    public virtual void OnOpen() { }

    /// <summary>Called each time the window is closed.</summary>
    public virtual void OnClose() { }

    /// <summary>
    /// Resolve a named binding to a string value.
    /// Return true and set <paramref name="value"/> when the binding is handled.
    /// </summary>
    public virtual bool GetBindingValue(ref string value, string bindingName) => false;

    /// <summary>
    /// Set a named binding value (called when the UI pushes data back to the controller).
    /// </summary>
    public virtual void SetBindingValue(string value, string bindingName) { }

    /// <summary>Ask the XUI system to re-read all binding values from this controller.</summary>
    public void RefreshBindings() { }
}

// ---------------------------------------------------------------------------
// XUI window / group
// ---------------------------------------------------------------------------

/// <summary>Represents a named XUI window group (corresponds to a &lt;window&gt; element).</summary>
public class XUiWindowGroup
{
    /// <summary>Gets or sets whether the window is visible.</summary>
    public bool isShown { get; set; }
}

// ---------------------------------------------------------------------------
// XUI root
// ---------------------------------------------------------------------------

/// <summary>
/// Root XUI manager for a player's UI. Provides access to named windows.
/// </summary>
public class XUi
{
    /// <summary>Returns the XUiWindowGroup with the given name, or null if not found.</summary>
    public XUiWindowGroup FindWindowGroupByName(string name) => null;
}

// ---------------------------------------------------------------------------
// Local player UI
// ---------------------------------------------------------------------------

/// <summary>
/// Entry point for accessing the UI of the local (human) player on the client.
/// </summary>
public class LocalPlayerUI
{
    private static readonly LocalPlayerUI _instance = new LocalPlayerUI();

    /// <summary>Returns the UI instance for the primary local player.</summary>
    public static LocalPlayerUI GetUIForPrimaryPlayer() => _instance;

    /// <summary>The XUI root for this player's UI.</summary>
    public XUi xui { get; } = new XUi();
}
