/* vi: ts=2
 * dotnet pack finally broke down and I couldn't get it working again.
 * This generates .NET packaging files
 * Copyright (C) Joshua Hudson 2025
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using ICSharpCode.SharpZipLib.Zip;

if (args.Length == 0)
{
	Console.WriteLine("Usage: NugetPacker csproj-file");
	Environment.Exit(1);
}

bool FileExists(string path)
{
	/* Too bad I can't call Emet.Filesystems from its own packager */
	try {
		using (var f = new FileStream(path, FileMode.Open, FileAccess.Read)){}
		return true;
	} catch (FileNotFoundException) {
		return false;
	}
}

var doc = new XmlDocument();
doc.Load(args[0]);

string relative = System.IO.Path.GetDirectoryName(args[0]);
string id = System.IO.Path.GetFileName(args[0]);
id = id.Substring(0, id.LastIndexOf('.'));
string version = null;
string authors = null;
string license = null;
string icon = null;
string readme = null;
string copyright = null;
string description = null;
string projecturl = null;
string[] targetframeworks = null;
string[] runtimeidentifiers = null;

SortedDictionary<string, string> packagefiles = new(StringComparer.OrdinalIgnoreCase);

foreach (XmlNode node in doc.DocumentElement.ChildNodes)
	if (node is XmlElement element)
	{
		switch (element.LocalName)
		{
			case "PropertyGroup":
				foreach (XmlNode node2 in element.ChildNodes)
					if (node2 is XmlElement property)
					{
						switch (property.LocalName)
						{
							case "TargetFramework": targetframeworks = [property.InnerText]; break;
							case "TargetFrameworks": targetframeworks = property.InnerText.Split(";"); break;
							case "RuntimeIdentifiers": runtimeidentifiers = property.InnerText.Split(";"); break;
							case "Version": version = property.InnerXml; break;
							case "Authors": authors = property.InnerXml; break;
							case "Copyright": copyright = property.InnerXml; break;
							case "Description": description = property.InnerXml; break;
							case "PackageIcon": icon = property.InnerXml; break;
							case "PackageProjectUrl": projecturl = property.InnerXml; break;
							case "PackageLicenseFile": license = property.InnerText; break;
							case "PackageReadmeFile": readme = property.InnerText; break;
						}
					}
				break;
			case "ItemGroup":
				foreach (XmlNode node3 in element.ChildNodes)
					if (node3 is XmlElement item && item.GetAttribute("Pack") == "true")
					{
						var include = item.GetAttribute("Include");
						if (include is null) continue;
						var packpath = Path.Combine(item.GetAttribute("PackagePath") ?? "", Path.GetFileName(include));
						packagefiles.Add(packpath.Replace('\\', '/'), Path.Combine(relative, include));
					}
				break;
		}
	}

if (version is null) {
	Console.Error.WriteLine("Version not se");
	Environment.Exit(1);
}

byte[] GenerateNuspec()
{
	var sb = new StringBuilder();
	sb.Append("<?xml version=\"1.0\" encoding=\"utf-8\">\n");
	sb.Append("<package xmlns=\"http://schemas.microsoft.com/packaging/2012/06/nuspec.xsd\">\n");
	sb.Append("  <metadata>\n");
	sb.Append("    <id>").Append(id).Append("</id>\n");
	sb.Append("    <version>").Append(version).Append("</version>\n");
	if (authors is not null) sb.Append("    <authors>").Append(authors).Append("</authors>\n");
	if (license is not null) {
		if (packagefiles.TryGetValue(license, out var licensepath)) {
			sb.Append("    <license type=\"file\">").Append(license).Append("</license>\n");
			sb.Append("    <licenseUrl>https://aka.ms/deprecateLicenseUrl</licenseUrl>\n");
		} else {
			Console.Error.WriteLine("License file not found");
			Environment.Exit(1);
		}
	}
	if (icon is not null) {
		if (packagefiles.TryGetValue(icon, out var iconpath)) {
			sb.Append("    <icon>").Append(icon).Append("</icon>\n");
		} else {
			Console.Error.WriteLine("Icon file not found");
			Environment.Exit(1);
		}
	}
	if (readme is not null) {
		if (packagefiles.TryGetValue(readme, out var readmepath)) {
			sb.Append("    <readme>").Append(readme).Append("</readme>\n");
		} else {
			Console.Error.WriteLine("Readme file not found");
			Environment.Exit(1);
		}
	}
  if (projecturl is not null) sb.Append("    <projectUrl>").Append(projecturl).Append("</projectUrl>\n");
	if (description is not null) sb.Append("    <description>").Append(description).Append("</description>\n");
	if (copyright is not null) sb.Append("    <copyright>").Append(copyright).Append("</copyright>\n");
	// sb.Append("    <repository type=\"git\"/>\n");
  sb.Append("    <dependencies>\n");
	foreach (var framework in targetframeworks)
	{
		string target = null;
		if (framework.StartsWith("netstandard"))
			target = ".NETStandard" + framework.Substring(11);
		else if (framework.StartsWith("net4"))
			target = ".NETFramework" + framework.Substring(3);
		else if (framework.StartsWith("netcore"))
			target = ".NETCore" + framework.Substring(7);
		else if (framework.StartsWith("net"))
			target += ".NET" + framework.Substring(3);
    // TODO Actually generate dependencies when it matters; need to parse project file for PackageReference
    sb.Append("      <group targetFramework=\"").Append(target).Append("\"/>\n");
	}
  sb.Append("    </dependencies>\n");
	sb.Append("  </metadata>\n</package>\n");
	return Encoding.UTF8.GetBytes(sb.ToString());
}

byte[] GenerateContentTypes()
{
	var seen = new HashSet<string>();
	var sb = new StringBuilder();
	seen.Add("rels");
	seen.Add("psmdcp");
	seen.Add("xml");
	sb.Append("<?xml version=\"1.0\" encoding=\"utf-8\">\n");
	sb.Append("<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">\n");
	sb.Append("  <Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>\n");
	sb.Append("  <Default Extension=\"psmdcp\" ContentType=\"application/vnd.openxmlformats-package.core-properties+xml\"/>\n");
	sb.Append("  <Default Extension=\"xml\" ContentType=\"application/xml\"/>\n");
	var overrides = new List<(string, string)>();
	foreach (var entry in packagefiles.Values.Order(StringComparer.Ordinal)) {
		int idx = entry.LastIndexOf('.');
		var e2 = (idx >= 0) ? entry.Substring(idx + 1) : "/" + entry;
		if (seen.Contains(e2)) continue;
		var type = (e2) switch {
			"nuspec" => "application/xml",
			"png" => "image/png",
			"jpg" => "image/jpeg",
			"webp" => "image/webp",
			"md" => "text/markdown; charset=UTF-8",
			"/LICENSE" => "text/plain; charset=us-ascii",
			_ => "application/octet-stream"};
		if (e2.StartsWith('/'))
			overrides.Add((e2, type));
		else
			sb.Append("  <Default Extension=\"").Append(e2).Append("\" ContentType=\"").Append(type).Append("\">\n");
		seen.Add(e2);
	}
	foreach (var (e2, type) in overrides)
		sb.Append("  <Override PartName=\"").Append(e2).Append("\" ContentType=\"").Append(type).Append("\">\n");
	sb.Append("</Types>");
	return Encoding.ASCII.GetBytes(sb.ToString());
}

byte[] GeneratePsmdcp()
{
	var sb = new StringBuilder();
	sb.Append("<?xml version=\"1.0\" encoding=\"utf-8\">\n");
	sb.Append("<coreProperties xmlns:dc=\"http://purl.org/dc/elements/1.1/\" xmlns:dcterms=\"http://purl.org/dc/terms/\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns=\"http://schemas.openxmlformats.org/package/2006/metadata/core-properties\">\n");
	if (authors is not null) sb.Append("  <dc:creator>").Append(authors).Append("</dc:creator>\n");
	if (description is not null) sb.Append("  <dc:description>").Append(description).Append("</dc:description>\n");
	sb.Append("  <dc:identifier>").Append(id).Append("</dc:identifier>\n");
	sb.Append("  <version>").Append(version).Append("</version>\n");
	sb.Append("  <keywords></keywords>\n"); // Not yet implemented
	sb.Append("  <lastModifiedBy>Emet.NugetPack, Version=0.0.0.1, Culture=neutral</lastModifiedBy>\n");
	sb.Append("</coreProperties>\n");
	return Encoding.UTF8.GetBytes(sb.ToString());
}

byte[] GenerateRels(string Ridentifier, string pidentifier)
{
	var sb = new StringBuilder();
	sb.Append("<?xml version=\"1.0\" encoding=\"utf-8\">\n");
	sb.Append("<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">\n");
	sb.Append(" <Relationship Type=\"http://schemas.microsoft.com/packaging/2010/07/manifest\" Target=\"/").Append(id);
	sb.Append(".nuspec\" Id=\"").Append(Ridentifier).Append("\"/>\n");
	sb.Append(" <Relationship Type=\"http://schemas.openxmlformats.org/package/2006/relationships/metadata/core-properties\" Target=\"/package/services/metadata/core-properties/").Append(pidentifier).Append(".psmdcp\" Id=\"").Append(Ridentifier).Append("\"/>\n");
	sb.Append("</Relationships>\n");
	return Encoding.UTF8.GetBytes(sb.ToString());
}

/*
  Length      Date    Time    Name
---------  ---------- -----   ----
      509  2025-07-12 06:09   _rels/.rels
      774  2025-07-12 06:09   Emet.FileSystems.nuspec
     1602  2025-07-12 06:09   LICENSE
      274  2025-07-12 06:09   emet.png
     2381  2025-07-12 06:09   README.md
    25600  2025-07-12 06:09   runtimes/linux-x64/lib/netstandard2.0/Emet.FileSystems.dll
    33280  2025-07-12 06:09   runtimes/win/lib/netstandard2.0/Emet.FileSystems.dll
    25600  2025-07-12 06:09   runtimes/osx-x64/lib/netstandard2.0/Emet.FileSystems.dll
    11264  2025-07-12 06:09   ref/netstandard2.0/Emet.FileSystems.dll
      649  2025-07-12 06:09   [Content_Types].xml
      718  2025-07-12 06:09   package/services/metadata/core-properties/e3b0c44298fc1c149afbf4c8996fb924.psmdcp
---------                     -------
   102651                     11 files

 */

var basename = Path.Combine(relative, "bin", "Release");
var dllname = id + ".dll";
var xmlname = id + ".xml";
foreach (var framework in targetframeworks)
{
	if (runtimeidentifiers?.Length > 0) {
		var name = Path.Combine(basename, framework, dllname);
		packagefiles.Add("ref/" + framework + "/" + dllname, name);
		name = Path.Combine(basename, framework, xmlname);
		if (FileExists(name)) packagefiles.Add("ref/" + framework + "/" + xmlname, name);
		foreach (var runtime in runtimeidentifiers)
		{
			name = Path.Combine(basename, framework, runtime, dllname);
			packagefiles.Add("runtimes/" + runtime + "/lib/" + framework + "/" + dllname, name);
			name = Path.Combine(basename, framework, runtime, xmlname);
			if (FileExists(name)) packagefiles.Add("runtimes/" + runtime + "/lib/" + framework + "/" + xmlname, name);
		}
	} else {
		var name = Path.Combine(basename, framework, dllname);
		packagefiles.Add("lib/" + framework + "/" + dllname, name);
		name = Path.Combine(basename, framework, xmlname);
		if (FileExists(name)) packagefiles.Add("lib/" + framework + "/" + xmlname, name);
	}
}

foreach (var p in packagefiles)
	Console.WriteLine(p.Key + " => " + p.Value);

// Get date
var sde = Environment.GetEnvironmentVariable("SOURCE_DATE_EPOCH");
DateTime date;
if (sde is null)
	date = DateTime.UtcNow;
else
	date = DateTime.UnixEpoch.AddSeconds(long.Parse(sde, System.Globalization.CultureInfo.InvariantCulture));

// Get nuspec
var nuspecbytes = GenerateNuspec();

// Get Rid and pid
var hashbytes = new byte[nuspecbytes.Length + 8];
System.Buffers.Binary.BinaryPrimitives.WriteInt64BigEndian(hashbytes, date.Ticks);
Array.Copy(nuspecbytes, 0, hashbytes, 8, nuspecbytes.Length);
var hash = System.Security.Cryptography.SHA256.HashData(hashbytes);
var Ridentifier = "R" + Convert.ToHexString(hash.AsSpan().Slice(hash.Length - 8, 8)).ToUpperInvariant();
var pidentifier = Convert.ToHexString(hash.AsSpan().Slice(0, 16)).ToLowerInvariant();

// Actually pack zip file
using (var o = new ZipOutputStream(new FileStream(Path.Combine(basename, id + "." + version + ".nupkg"), FileMode.Create, FileAccess.Write)))
{
	o.SetLevel(9);
	void PushFile(string name, byte[] data)
	{
		var entry = new ZipEntry(name);
		entry.DateTime = date;
		entry.Size = data.Length;
		o.PutNextEntry(entry);
		o.Write(data, 0, data.Length);
	}
	PushFile("[Content_Types].xml", GenerateContentTypes());
	PushFile("_rels/.rels", GenerateRels(Ridentifier, pidentifier));
	PushFile("packages/services/metadata/core-properties/" + pidentifier + ".psmdcp", GeneratePsmdcp());
	PushFile(id + ".nuspec", nuspecbytes);
	foreach (var (name, path) in packagefiles)
		PushFile(name, File.ReadAllBytes(path));
	o.Close();
}
