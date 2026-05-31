using System; using System.Reflection;
var api=Assembly.LoadFrom(args[0]);
foreach(var tname in new[]{"WebRTCme.RTCIceServer","WebRTCme.RTCConfiguration"}){var t=api.GetType(tname); Console.WriteLine("--"+tname); foreach(var p in t.GetProperties()) Console.WriteLine($"{p.Name} {p.PropertyType}");}
