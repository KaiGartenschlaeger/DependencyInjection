using System;

namespace KeepSimple.DependencyInjection
{
    /// <summary>
    /// Definiert das die Abhängigkeiten automatisch aufgelöst werden sollen. Siehe <see cref="IDependencyResolver"/>.
    /// <para>
    /// Wird bei unterschiedlichen Konstruktoren verwendet, um den Konstruktor für die automatische Auflösung anzugeben.
    /// </para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public class DependencyAttribute : Attribute
    {

    }
}