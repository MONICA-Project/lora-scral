#if !NETCOREAPP
using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;

// Allgemeine Informationen über eine Assembly werden über die folgenden
// Attribute gesteuert. Ändern Sie diese Attributwerte, um die Informationen zu ändern,
// die einer Assembly zugeordnet sind.
[assembly: AssemblyTitle("Lora-Scral")]
[assembly: AssemblyDescription("Pushes Loratracker data to Monica Scral endpoint")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Fraunhofer FIT")]
[assembly: AssemblyProduct("Lora-Scral")]
[assembly: AssemblyCopyright("Copyright © Fraunhofer FIT, BlubbFish 2019 - 31.05.2019")]
[assembly: AssemblyTrademark("Fraunhofer FIT, BlubbFish")]
[assembly: AssemblyCulture("")]
[assembly: NeutralResourcesLanguage("de-DE")]

// Durch Festlegen von ComVisible auf FALSE werden die Typen in dieser Assembly
// für COM-Komponenten unsichtbar.  Wenn Sie auf einen Typ in dieser Assembly von
// COM aus zugreifen müssen, sollten Sie das ComVisible-Attribut für diesen Typ auf "True" festlegen.
[assembly: ComVisible(false)]

// Die folgende GUID bestimmt die ID der Typbibliothek, wenn dieses Projekt für COM verfügbar gemacht wird
[assembly: Guid("af25b9bc-7d93-4cec-b5e2-8a8383679787")]

// Versionsinformationen für eine Assembly bestehen aus den folgenden vier Werten:
//
//      Hauptversion
//      Nebenversion
//      Buildnummer
//      Revision
//
// Sie können alle Werte angeben oder Standardwerte für die Build- und Revisionsnummern verwenden,
// übernehmen, indem Sie "*" eingeben:
// [assembly: AssemblyVersion("1.0.*")]
[assembly: AssemblyVersion("1.0.1")]
[assembly: AssemblyFileVersion("1.0.1")]
#endif
/*
 * 1.0.0 First Version
 * 1.0.1 Update to the new ConnectionDataMqtt
 */
