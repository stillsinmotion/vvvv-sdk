﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Reflection;

using VVVV.Core;
using VVVV.Core.Logging;
using VVVV.Core.Model;
using VVVV.Core.Model.CS;
using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;

namespace VVVV.Hosting.Factories
{
	[Export(typeof(IAddonFactory))]
	[Export(typeof(EditorFactory))]
	public class EditorFactory : IAddonFactory, IMouseClickListener
	{
		[ImportMany(typeof(IEditor), AllowRecomposition = true)]
		protected List<ExportFactory<IEditor, IEditorInfo>> FChangingNodeInfoExports;
		protected List<ExportFactory<IEditor, IEditorInfo>> FNodeInfoExports;
		
		[Import]
		protected INodeInfoFactory FNodeInfoFactory;
		
		[Import]
		protected ISolution FSolution;
		
		private ILogger FLogger;
		private IHDEHost FHDEHost;
		private CompositionContainer FContainer;
		private HostExportProvider FHostExportProvider;
		private ExportProvider[] FExportProviders;
		private Dictionary<INodeInfo, ExportFactory<IEditor, IEditorInfo>> FNodeInfos;
		private Dictionary<IInternalPluginHost, ExportLifetimeContext<IEditor>> FExportLifetimeContexts;
		private int FMoveToLine;
		private INode FNodeToAttach;
		
		[ImportingConstructor]
		public EditorFactory(CompositionContainer parentContainer, ILogger logger, IHDEHost hdeHost)
		{
			FHostExportProvider = new HostExportProvider();
			FExportProviders = new ExportProvider[] { parentContainer, FHostExportProvider };
			FNodeInfos = new Dictionary<INodeInfo, ExportFactory<IEditor, IEditorInfo>>();
			FExportLifetimeContexts = new Dictionary<IInternalPluginHost, ExportLifetimeContext<IEditor>>();
			FNodeInfoExports = new List<ExportFactory<IEditor, IEditorInfo>>();
			FLogger = logger;
			FHDEHost = hdeHost;
			FMoveToLine = -1;
			
			FHDEHost.AddListener(this);
		}
		
		public IEnumerable<INodeInfo> ExtractNodeInfos(string filename, string arguments)
		{
			// Present the user with all files associated with this filename.
			var nodeInfo = CreateNodeInfo(filename);
			if (nodeInfo != null)
				yield return nodeInfo;
			
			// Do we have a project file?
			// TODO: Do not hardcode project extension.
			if (Path.GetExtension(filename) == ".csproj")
			{
				var project = FSolution.FindProject(filename);
				if (project != null)
				{
					if (!project.IsLoaded)
						project.Load();
					
					foreach (var doc in project.Documents)
					{
						var docFilename = doc.Location.LocalPath;
						
						if (docFilename != filename)
						{
							nodeInfo = CreateNodeInfo(doc.Location.LocalPath);
							if (nodeInfo != null)
								yield return nodeInfo;
						}
					}
				}
			}
		}
		
		private INodeInfo CreateNodeInfo(string filename)
		{
			var fileExtension = Path.GetExtension(filename);
			
			foreach (var nodeInfoExport in FNodeInfoExports)
			{
				if (nodeInfoExport.Metadata.FileExtensions.Contains(fileExtension))
				{
					var nodeInfo = FNodeInfoFactory.CreateNodeInfo(
						Path.GetFileName(filename),
						"Editor",
						string.Empty,
						filename);
					
					nodeInfo.BeginUpdate();
					nodeInfo.Type = NodeType.Text;
					nodeInfo.Ignore = true;
					nodeInfo.AutoEvaluate = true;
					nodeInfo.InitialBoxSize = new System.Drawing.Size(400, 300);
					nodeInfo.InitialWindowSize = new System.Drawing.Size(800, 600);
					nodeInfo.InitialComponentMode = TComponentMode.InAWindow;
					nodeInfo.CommitUpdate();
					
					FNodeInfos[nodeInfo] = nodeInfoExport;
					
					return nodeInfo;
				}
			}
			
			return null;
		}
		
		public bool Create(INodeInfo nodeInfo, IAddonHost host)
		{
			bool result = false;
			
			var editorHost = host as IInternalPluginHost;
			
			if (editorHost != null && FNodeInfos.ContainsKey(nodeInfo))
			{
				IEnumerable<KeyValuePair<IInternalPluginHost, ExportLifetimeContext<IEditor>>> entries = null;
				if (FNodeToAttach != null)
				{
					// Try to find an existing editor which is already attached to this node
					// and has this file openend.
					entries =
						from entry in FExportLifetimeContexts
						let e = entry.Value.Value
						where new Uri(e.OpenedFile) == new Uri(nodeInfo.Filename)
						where e.AttachedNode == FNodeToAttach
						select entry;
				}
				else
				{
					// Try to find an existing editor which has opened this file and is not
					// attached to any node.
					entries =
						from entry in FExportLifetimeContexts
						let e = entry.Value.Value
						where new Uri(e.OpenedFile) == new Uri(nodeInfo.Filename)
						where e.AttachedNode == null
						select entry;
				}
				
				if (entries.Any())
				{
					return ShowEditor(entries.FirstOrDefault());
				}
				
				// We didn't find a suitable editor, create a new one.
				FHostExportProvider.PluginHost = host as IPluginHost2;
				
				var nodeInfoExport = FNodeInfos[nodeInfo];
				var exportLifetimeContext = nodeInfoExport.CreateExport();
				FExportLifetimeContexts[editorHost] = exportLifetimeContext;
				
				var editor = exportLifetimeContext.Value;
				editorHost.Plugin = editor;
				editor.Open(nodeInfo.Filename);
				
				if (FNodeToAttach != null)
					editor.AttachedNode = FNodeToAttach;
				
				if (FMoveToLine >= 0)
					editor.MoveTo(FMoveToLine);
				
				result = true;
				
				FMoveToLine = -1;
				FNodeToAttach = null;
			}
			
			return result;
		}
		
		private bool ShowEditor(KeyValuePair<IInternalPluginHost, ExportLifetimeContext<IEditor>> entry)
		{
			var editor = entry.Value.Value;
			var editorNode = entry.Key as INode;
			
			if (FNodeToAttach != null)
				editor.AttachedNode = FNodeToAttach;
			
			if (FMoveToLine >= 0)
				editor.MoveTo(FMoveToLine);
			
			FHDEHost.ShowGUI(editorNode);
			return true;
		}
		
		public bool Delete(INodeInfo nodeInfo, IAddonHost host)
		{
			var editorHost = host as IInternalPluginHost;
			
			if (editorHost != null && FNodeInfos.ContainsKey(nodeInfo) && FExportLifetimeContexts.ContainsKey(editorHost))
			{
				var exportLifetimeContext = FExportLifetimeContexts[editorHost];
				exportLifetimeContext.Dispose();
				FExportLifetimeContexts.Remove(editorHost);
			}
			
			return false;
		}
		
		public bool Clone(INodeInfo nodeInfo, string path, string name, string category, string version, out INodeInfo newNodeInfo)
		{
			// TODO: What to do here?
			newNodeInfo = null;
			return false;
		}
		
		public event NodeInfoEventHandler NodeInfoAdded;
		
		protected virtual void OnNodeInfoAdded(INodeInfo nodeInfo)
		{
			if (NodeInfoAdded != null)
				NodeInfoAdded(this, nodeInfo);
		}
		
		public event NodeInfoEventHandler NodeInfoRemoved;
		
		protected virtual void OnNodeInfoRemoved(INodeInfo nodeInfo)
		{
			if (NodeInfoRemoved != null)
				NodeInfoRemoved(this, nodeInfo);
		}
		
		public string JobStdSubPath {
			get {
				return "editors";
			}
		}
		
		public void AddDir(string dir, bool recursive)
		{
			try
			{
				var catalog = new DirectoryCatalog(dir);
				FContainer = new CompositionContainer(catalog, FExportProviders);
				FContainer.ComposeParts(this);
				
				FNodeInfoExports.AddRange(FChangingNodeInfoExports);
			}
			catch (ReflectionTypeLoadException e)
			{
				foreach (var f in e.LoaderExceptions)
					FLogger.Log(f);
				return;
			}
			catch (Exception e)
			{
				FLogger.Log(e);
				return;
			}
		}
		
		public void RemoveDir(string dir)
		{
			// Nothing todo. We didn't emit any new node info.
		}

		public event NodeInfoEventHandler NodeInfoUpdated;
		
		protected virtual void OnNodeInfoUpdated(IAddonFactory factory, INodeInfo nodeInfo)
		{
			if (NodeInfoUpdated != null) {
				NodeInfoUpdated(factory, nodeInfo);
			}
		}

		public void MouseDownCB(INode node, Mouse_Buttons button, Modifier_Keys keys)
		{
			if (node == null) return;
			
			if ((button == Mouse_Buttons.Left) && (keys == Modifier_Keys.Control))
			{
				// Let the user choose which file to open.
				
				var nodeInfo = node.GetNodeInfo();
				
				switch (nodeInfo.Type)
				{
					case NodeType.Text:
					case NodeType.Dynamic:
					case NodeType.Effect:
						Open(nodeInfo.Filename, -1, node);
						break;
				}
			}
			else if (button == Mouse_Buttons.Right)
			{
				// Try to locate exact file based on nodeinfo and navigate to its definition.
				
				var nodeInfo = node.GetNodeInfo();
				
				switch (nodeInfo.Type)
				{
					case NodeType.Text:
					case NodeType.Dynamic:
					case NodeType.Effect:
						var filename = nodeInfo.Filename;
						var line = -1;
						
						// Do we have a project file?
						var project = FSolution.FindProject(filename);
						if (project != null && project is CSProject)
						{
							// Find the document where this nodeinfo is defined.
							var doc = FindDefiningDocument(project as CSProject, nodeInfo);
							if (doc != null)
							{
								filename = doc.Location.LocalPath;
								line = FindDefiningLine(doc, nodeInfo);
							}
						}
						
						Open(filename, line, node);
						break;
				}
			}
		}
		
		public void Open(string filename)
		{
			Open(filename, -1);
		}
		
		public void Open(string filename, int line)
		{
			Open(filename, line, null);
		}
		
		public void Open(string filename, int line, INode nodeToAttach)
		{
			Open(filename, line, nodeToAttach, null);
		}
		
		public void Open(string filename, int line, INode nodeToAttach, IWindow window)
		{
			// See if we can find an editor already attached to this node.
			if (nodeToAttach != null)
			{
				var entries =
					from entry in FExportLifetimeContexts
					let e = entry.Value.Value
					where e.AttachedNode == nodeToAttach
					select entry;
				
				if (entries.Any())
				{
					foreach (var entry in entries)
					{
						var editorNode = entry.Key as INode;
						var editor = entry.Value.Value;
						
						if (new Uri(editor.OpenedFile) == new Uri(filename))
						{
							if (line >= 0)
								editor.MoveTo(line);
							
							FHDEHost.ShowGUI(editorNode);
							return;
						}
					}
					
					if (window == null)
					{
						// Take the Window of any attached editor and open it there.
						var editorNode = entries.First().Key as INode;
						window = editorNode.Window;
					}
				}
			}
			
//			if (window == null)
//			{
//				// Before we open the editor in a new window, see if the file to
//				// open is a project file and search for an editor already
//				// editing this project.
//				// If we find one, open the editor there.
//				var project = FSolution.FindProject(filename);
//				if (project != null && project.IsLoaded)
//				{
//					foreach (var doc in project.Documents)
//					{
//						var docFilename = doc.Location.LocalPath;
//
//						var editorNodes =
//							from entry in FExportLifetimeContexts
//							let e = entry.Value.Value
//							where new Uri(e.OpenedFile) == new Uri(docFilename)
//							select entry.Key as INode;
//
//						var editorNode = editorNodes.First();
//						if (editorNode != null)
//						{
//							window = editorNode.Window;
//							break;
//						}
//					}
//				}
//			}
			
			// The following Open will trigger a call by vvvv to IInternalHDEHost.ExtractNodeInfos()
			// Force the hde host to collect node infos from us only.
			var addonFactories = new List<IAddonFactory>(FHDEHost.AddonFactories);
			try
			{
				FMoveToLine = line;
				FNodeToAttach = nodeToAttach;
				
				FHDEHost.AddonFactories.Clear();
				FHDEHost.AddonFactories.Add(this);
				FHDEHost.Open(filename, false, window);
			}
			finally
			{
				FHDEHost.AddonFactories.Clear();
				FHDEHost.AddonFactories.AddRange(addonFactories);
			}
		}
		
		public void MouseUpCB(INode node, Mouse_Buttons button, Modifier_Keys keys)
		{
		}
		
		protected CSDocument FindDefiningDocument(CSProject project, INodeInfo nodeInfo)
		{
			foreach (var doc in project.Documents)
			{
				var csDoc = doc as CSDocument;
				if (csDoc == null) continue;
				
				if (FindDefiningLine(csDoc, nodeInfo) >= 0)
					return csDoc;
			}
			
			return null;
		}
		
		protected int FindDefiningLine(CSDocument document, INodeInfo nodeInfo)
		{
			var parseInfo = document.ParseInfo;
			var compilationUnit = parseInfo.MostRecentCompilationUnit;
			if (compilationUnit == null) return -1;
			
			foreach (var clss in compilationUnit.Classes)
			{
				foreach (var attribute in clss.Attributes)
				{
					var attributeType = attribute.AttributeType;
					var pluginInfoName = typeof(PluginInfoAttribute).Name;
					var pluginInfoShortName = pluginInfoName.Replace("Attribute", "");
					if (attributeType.Name == pluginInfoName || attributeType.Name == pluginInfoShortName)
					{
						// Check name
						string name = null;
						if (attribute.NamedArguments.ContainsKey("Name"))
							name = (string) attribute.NamedArguments["Name"];
						else if (attribute.PositionalArguments.Count >= 0)
							name = (string) attribute.PositionalArguments[0];
						
						if (name != nodeInfo.Name)
							continue;
						
						// Check category
						string category = null;
						if (attribute.NamedArguments.ContainsKey("Category"))
							category = (string) attribute.NamedArguments["Category"];
						else if (attribute.PositionalArguments.Count >= 1)
							category = (string) attribute.PositionalArguments[1];
						
						if (category != nodeInfo.Category)
							continue;

						// Possible match
						bool match = true;
						
						// Check version
						if (nodeInfo.Version != null)
						{
							string version = null;
							if (attribute.NamedArguments.ContainsKey("Version"))
								version = (string) attribute.NamedArguments["Version"];
							else if (attribute.PositionalArguments.Count >= 2)
								version = (string) attribute.PositionalArguments[2];
							
							match = version == nodeInfo.Version;
						}
						
						if (match)
							return attribute.Region.BeginLine;
					}
				}
			}
			
			return -1;
		}
	}

	public interface IEditorInfo
	{
		string[] FileExtensions { get; }
	}
}
