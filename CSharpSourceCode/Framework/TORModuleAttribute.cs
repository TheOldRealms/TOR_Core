using System;

namespace TOR_Core.Framework
{
    /// <summary>
    /// Marks an <see cref="ITORModule"/> implementation as a module for future
    /// reflection-based discovery/registration (see docs/vertical-slicing-proposal.md) -
    /// the same pattern Extensions/UI's ViewModelExtensionManager already uses for
    /// [ViewModelExtension]. No discovery/registry consumes this attribute yet; modules are
    /// still registered by an explicit call from SubModule.cs. Tagging a module with this
    /// now costs nothing and documents intent for when that registry is built.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class TORModuleAttribute : Attribute
    {
    }
}
