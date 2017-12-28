﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Runtime.InteropServices;


using EAAddinFramework.Utilities;
using UML=TSF.UmlToolingFramework.UML;

namespace TSF.UmlToolingFramework.Wrappers.EA
{
	/// <summary>
	/// Description of Package.
	/// </summary>
	public class Package:ElementWrapper, UML.Classes.Kernel.Package
	{
		
		private Boolean dontSave = false;
		private string _fqn = string.Empty;
		internal global::EA.Package wrappedPackage {get;set;}
		public override global::EA.Element WrappedElement 
		{
			get 
			{
				if(this.wrappedElement == null)
				{
					this.wrappedElement = this.EAModel.wrappedModel.GetElementByGuid(this.guid);
				}
				return base.wrappedElement;
			}
			set {
				base.WrappedElement = value;
			}
		} 
		public int packageID
		{
			get {return this.wrappedPackage.PackageID;}
		}
		public override string uniqueID
		{
			get {return this.wrappedPackage.PackageGUID;}
		}
		public Package(Model model,global::EA.Package package):base(model,Package.getElementForPackage(model,package))
		{
			this.initialize(package);
		}
		public List<T> getOwnedElementWrappers<T>( string stereotype, bool recursive) where T:ElementWrapper
		{
			string packageIDString = recursive ? this.getPackageTreeIDString() : this.packageID.ToString();
			string getGetOwnedElements = "select * from t_object o" +
										" inner join t_xref x on x.Client = o.ea_guid " +
										" where o.Object_Type = '" + typeof(T).Name  + "' " +
										" and o.Package_ID in (" + packageIDString + ") " +
										" and x.Name = 'Stereotypes' " +
										" and x.Description like '%@STEREO;Name=" + stereotype + ";%'";
			return EAModel.getElementWrappersByQuery(getGetOwnedElements).Cast<T>().ToList();
		}
		public static global::EA.Element getElementForPackage(Model model, global::EA.Package package)
		{
			global::EA.Element foundElement = package.Element;
			//if for some reason the Element is not filled in we get it using the package GUID.
			if (foundElement == null)
			{
				foundElement = model.wrappedModel.GetElementByGuid(package.PackageGUID);
			}
			return foundElement;
		}
		protected void initialize(global::EA.Package package)
		{
			base.initialize(Package.getElementForPackage(this.EAModel,package));
			this.wrappedPackage = package;
			if (string.IsNullOrEmpty(this._uniqueID)) this._uniqueID = package.PackageGUID;
		}
		public override String notes 
		{
	        get { return this.WrappedPackage.Notes;  }
	        set { this.WrappedPackage.Notes = value; }
    	}
		public global::EA.Package WrappedPackage 
		{
			get { return this.wrappedPackage; }
		}
		public override UML.Classes.Kernel.Element owner {
			get 
			{
				return base.owner;
			}
			set 
			{
				base.owner = value;
				this.WrappedPackage.ParentID = ((Package)value).packageID;
			}
		}
		public override UML.Classes.Kernel.Package owningPackage 
		{
			get 
			{
				return base.owningPackage;
			}
			set 
			{ try
				{
					this.wrappedPackage.ParentID = ((Package)value).packageID;
					//set the owner as well
					this.owner = value;
				}
				catch(COMException e)
				{
					//find the new parent package
					this.save();
					var newParent = (Package)this.EAModel.getElementWrapperByPackageID(((Package)value).packageID);
					if (newParent != null)
					{
						string sqlUpdatePackageParent =   "update t_package set Parent_ID = " + newParent.packageID
							+ " where ea_guid = '"+ this.uniqueID +"'";
						EAModel.executeSQL(sqlUpdatePackageParent);
						string sqlUpdateObjectParent =   "update t_object set Package_ID = " + newParent.packageID
							+ " where ea_guid = '"+ this.uniqueID +"'";
						EAModel.executeSQL(sqlUpdateObjectParent);
						//reload from the database
						this._owner = (Package)value;
						this.dontSave = true;
						//DEBUG: reload
						this.reload();
					}
				}
			}
		}
		public void reload()
		{
			this.EAModel.wrappedModel.RefreshModelView(0);
			global::EA.Package eaPackage = this.EAModel.wrappedModel.GetPackageByGuid(this.uniqueID);
			Logger.log("ParentID of eaPackage "+ eaPackage.Name + " = " +eaPackage.ParentID);
			this.initialize(eaPackage);
		}
		/// <summary>
		/// returns the package at the root of this model branch
		/// </summary>
		/// <returns>package at the root of this model branch</returns>
		public UML.Classes.Kernel.Package getRootPackage()
		{
			var ownerPackage = this.owner as Package;
			if (ownerPackage != null)
			{
				return ownerPackage.getRootPackage();
			}
			else
			{
				return this;
			}
		}
		public TSF.UmlToolingFramework.UML.Classes.Kernel.PackageableElement ownedMembers {
			get {
				throw new NotImplementedException();
			}
			set {
				throw new NotImplementedException();
			}
		}
		
		public HashSet<TSF.UmlToolingFramework.UML.Classes.Kernel.Type> ownedTypes {
			get {
				throw new NotImplementedException();
			}
			set {
				throw new NotImplementedException();
			}
		}
		
		public HashSet<UML.Classes.Kernel.Package> nestedPackages 
		{
			get 
			{
				this.wrappedPackage.Packages.Refresh(); // make sure that the most up to date list of packages
				return new HashSet<UML.Classes.Kernel.Package>(this.ownedElements.OfType<UML.Classes.Kernel.Package>());
			}
			set {
				throw new NotImplementedException();
			}
		}
		
		public TSF.UmlToolingFramework.UML.Classes.Kernel.Package nestingPackage {
			get {
				throw new NotImplementedException();
			}
			set {
				throw new NotImplementedException();
			}
		}
		
		public HashSet<TSF.UmlToolingFramework.UML.Classes.Kernel.PackageMerge> packageMerges {
			get {
				throw new NotImplementedException();
			}
			set {
				throw new NotImplementedException();
			}
		}

		public bool isEmpty 
		{
			get 
			{
				string sqlIsEmpty = @"select 'true' as isEmpty from (((t_package p
									left join t_object o on o.Package_ID = p.Package_ID)
									left join t_diagram d on d.Package_ID = p.Package_ID)
									left join t_package p_sub on p_sub.[Parent_ID] = p.[Package_ID])
									where o.Object_ID is null
									and d.Diagram_ID is null
									and  p.Package_ID = " + this.packageID;
				var isEmptyXml = this.EAModel.SQLQuery(sqlIsEmpty);
				var isEmptyNode = isEmptyXml.SelectSingleNode(this.EAModel.formatXPath("//isEmpty"));
				return isEmptyNode != null && isEmptyNode.InnerText.Equals("true",StringComparison.InvariantCultureIgnoreCase);
			}
		}
		/// <summary>
		/// 
		/// </summary>
		public override HashSet<UML.Classes.Kernel.Element> ownedElements {
			get 
			{ 
        this.wrappedPackage.Elements.Refresh();
				List<UML.Classes.Kernel.Element> elements = this.EAModel.factory.createElements( this.wrappedPackage.Elements).Cast<UML.Classes.Kernel.Element>().ToList();
				elements.AddRange(this.EAModel.factory.createElements( this.wrappedPackage.Packages).Cast<UML.Classes.Kernel.Element>());
				return new HashSet<UML.Classes.Kernel.Element>(elements);
			}
			set 
			{ 
				throw new NotImplementedException();
			}
		}
		public override HashSet<TSF.UmlToolingFramework.UML.Diagrams.Diagram> ownedDiagrams 
		{
			get 
			{
	    		HashSet<TSF.UmlToolingFramework.UML.Diagrams.Diagram> diagrams = new HashSet<TSF.UmlToolingFramework.UML.Diagrams.Diagram>();
	    		foreach ( var eaDiagram in this.wrappedPackage.Diagrams)
	    		{
	    			var newDiagram = ((Factory)this.EAModel.factory).createDiagram(eaDiagram);
	    			if (newDiagram != null) diagrams.Add(newDiagram);
	    		}
	    		return diagrams;
			}
			set {throw new NotImplementedException();}
		}
		public override string fqn 
		{
			get 
			{  
				if (this._fqn == string.Empty)
				{
					this._fqn = this.getFQN(string.Empty, this.packageID);
				}
				return this._fqn;
			}
		}
		public override string guid 
		{
			get 
			{
				return this.wrappedPackage.PackageGUID;
			}
		}
		/// <summary>
		/// gets the FQN without the need of instantiating all the owners up to the root element.
		/// From a package we are sure that only packages are the owners so its safe to check only t_package.
		/// </summary>
		/// <param name="fqnString">the fqn string so far</param>
		/// <param name="parentID">the id of the package</param>
		/// <returns>the fqn of the package</returns>
		private string getFQN(string fqnString, int packageID)
		{
			string newFQN = fqnString;
			XmlDocument result = this.EAModel.SQLQuery("select p.Parent_ID, p.Name from t_package p where p.Package_ID = " + packageID.ToString());
			XmlNode parentIDNode = result.SelectSingleNode(this.EAModel.formatXPath("//Parent_ID"));
			XmlNode nameNode = result.SelectSingleNode(this.EAModel.formatXPath("//Name"));
			if (nameNode != null)
			{
				//add the "." if necesarry
				if (newFQN != string.Empty)
				{
					newFQN = "." + newFQN;	
				}
				newFQN = nameNode.InnerText + newFQN;
				
				int parentID;
				if (parentIDNode != null && int.TryParse(parentIDNode.InnerText, out parentID))
				{
					newFQN = getFQN(newFQN, parentID);
				}
			}
			return newFQN;
		}
		/// creates a new element of the given type as an owned element of this 
	    /// element
	    public override T addOwnedElement<T>(String name, string EAType) 
	    {	
	      System.Type type = typeof(T);
	      T newElement;
		  
	      if(((Factory)this.EAModel.factory).isEAAtttribute(type)) 
	      {
	      	throw new Exception("Cannot add an Attribute to a Package");
	      } 
	      else if(((Factory)this.EAModel.factory).isEAOperation(type))
	      {
	        throw new Exception("Cannot add an Operation to a Package");
	      }
	      else if (((Factory)this.EAModel.factory).isEAPackage(type))
	      {
	        newElement = ((Factory)this.EAModel.factory).addElementToEACollection<T>
	          ( this.wrappedPackage.Packages, name, EAType  );
	      } 
	      else if (((Factory)this.EAModel.factory).isEAConnector(type))
	      {
	        newElement = ((Factory)this.EAModel.factory).addElementToEACollection<T>
	          ( this.wrappedPackage.Connectors, name, EAType  );
	      } 
	      else 
	      {
	        newElement = ((Factory)this.EAModel.factory).addElementToEACollection<T>
	          ( this.wrappedPackage.Elements, name, EAType );
	      }
	      return newElement;
	    }
	    /// <summary>
	    /// creates a new diagram under this package
	    /// </summary>
	    /// <param name="name">the name of the new diagram</param>
	    /// <returns>the new diagram</returns>
		public override T addOwnedDiagram<T>(string name)
		{
			return ((Factory)this.EAModel.factory).addNewDiagramToEACollection<T>(this.wrappedPackage.Diagrams,name);
		}
		public override void save()
		{
			if (! this.dontSave) this.wrappedPackage.Update();
		}
		/// <summary>
		/// deletes an element owned by this Package
		/// </summary>
		/// <param name="ownedElement">the owned element to delete</param>
		public override void deleteOwnedElement(Element ownedElement)
		{
			if (ownedElement is Package)
			{
				for (short i = 0; i< this.WrappedPackage.Packages.Count; i++)
				{
					var eaPackage = this.WrappedPackage.Packages.GetAt(i) as global::EA.Package;
					if (eaPackage.PackageGUID == ownedElement.guid)
					{   
						this.WrappedPackage.Packages.Delete(i);
						this.WrappedPackage.Packages.Refresh();
						break;
					}
				}
			}
			else if (ownedElement is ElementWrapper)
			{
				for (short i = 0; i< this.WrappedPackage.Elements.Count; i++)
				{
					var eaElement = this.WrappedPackage.Elements.GetAt(i) as global::EA.Element;
					if (eaElement.ElementGUID == ownedElement.guid)
					{   
						this.WrappedPackage.Elements.Delete(i);
						this.WrappedPackage.Elements.Refresh();
						break;
					}
				}
			}
			else
			{
				//currently only deleting elements is supported
				throw new NotImplementedException();
			}
		}
		/// <summary>
		/// export this package to xmi in the default format
		/// </summary>
		/// <param name="filePath">the filepath to save the xmi file to</param>
		public void exportToXMI(string filePath,bool includeDiagrams)
		{
			this.EAModel.wrappedModel.SuppressEADialogs = true;
			var projectInterface = this.EAModel.wrappedModel.GetProjectInterface();
			string xmlGUID = projectInterface.GUIDtoXML(this.guid);
			int diagramsPar = includeDiagrams ? 3 : -1;
			projectInterface.ExportPackageXMI(xmlGUID,global::EA.EnumXMIType.xmiEADefault,2,diagramsPar,1,0,filePath);
			this.EAModel.wrappedModel.SuppressEADialogs = false;
		}


		public void refresh()
		{
			this.EAModel.wrappedModel.RefreshModelView(this.packageID);
		}
		public override List<UML.Extended.UMLItem> findOwnedItems(string itemDescriptor)
		{
			List<UML.Extended.UMLItem> foundItems = new List<UML.Extended.UMLItem>();
			//first try to find it in a faster way
			//get the idstrings of this package and all its owned packages
			string packageTreeIDString = this.getPackageIDString(getNestedPackageTree(true));
			//get the individual parts
			var descriptorParts = itemDescriptor.Split('.').ToList();
			//if there's only one part then look for an element with that name, then for a diagram
			if (descriptorParts.Count == 1) 
			{
				//look for element
				foundItems.AddRange(getOwnedElements(descriptorParts[0], packageTreeIDString));
				//look for a diagram
				foundItems.AddRange(getOwnedDiagrams(descriptorParts[0],packageTreeIDString));
			}
			else if (descriptorParts.Count == 2)
			{
				//we take the first two items and try to find a match
				string ownerName = descriptorParts[0];
				string attributeName = descriptorParts[1];
				//first look for an attribute
				foundItems.AddRange(getOwnedAttributes(ownerName,attributeName,packageTreeIDString));
			}
			if (descriptorParts.Count >= 2 
			      && !foundItems.Any())
			{
				//top down approach
				//start by the elemnts directly owned by the package
				foundItems.AddRange(findOwnedItems(descriptorParts));
				if (! foundItems.Any())
				{
					//if still nothing found then we start by all elements somewhere in the package tree that match the first part
					var candidates = this.getOwnedElements(descriptorParts[0], packageTreeIDString);
					foreach (var candiate in candidates) 
					{
						foundItems.AddRange(candiate.findOwnedItems(descriptorParts));
					}
				}
				
			}
			//if still nothing found then get the base implemetation
			if (foundItems.Count == 0) foundItems.AddRange(base.findOwnedItems(itemDescriptor));
			return foundItems;
		}
		
		public override List<UML.Extended.UMLItem> findOwnedItems(List<String> descriptionParts)
		{
			List<UML.Extended.UMLItem> ownedItems =new List<UML.Extended.UMLItem>();
			if (descriptionParts.Count > 0)
			{
				string firstpart = descriptionParts[0];
					//start by finding an element with the given name
				var directOwnedElements = getOwnedElements(firstpart,this.packageID.ToString());
				if (descriptionParts.Count > 1)
				{
					//loop the owned elements and get their owned items 
					foreach (var element in directOwnedElements) 
					{
						//remove the first part
						descriptionParts.RemoveAt(0);         
						//go one level down
						ownedItems.AddRange(element.findOwnedItems(descriptionParts));
					}
				}
				else
				{
					//only one item so add the direct owned elements
					ownedItems.AddRange(directOwnedElements);
					//Add also the diagrams owned by this package
					ownedItems.AddRange(getOwnedDiagrams(firstpart,this.packageID.ToString()));
				}
			}
			return ownedItems;
		}
		
		public List<Attribute> getOwnedAttributes(string ownerName,string attributeName, string packageIDList)
		{
			//owner.Attribute
			string sqlGetAttributes = @"select a.ea_guid from (t_attribute a
										inner join t_object o on o.Object_ID = a.Object_ID)
										where a.Name = '" + attributeName + @"'
										and o.Name = '" + ownerName + @"'
										and o.Package_ID in (" + packageIDList + @")";

			return this.EAModel.getAttributesByQuery(sqlGetAttributes);
		}

		public List<ElementWrapper> getOwnedElements(string elementName, string packageIDList)
		{
			string sqlGetOwnedElement = "select o.Object_ID from t_object o " +
										" where " +
										" o.Name = '" + elementName + "' " +
				" and o.Package_ID in (" + packageIDList + ") ";
			return this.EAModel.getElementWrappersByQuery(sqlGetOwnedElement);
		}
		public List<Diagram> getOwnedDiagrams(string diagramName, string packageIDList)
		{
			string sqlGetOwnedDiagram = "select d.Diagram_ID from t_diagram d " +
										" where " +
										" d.Name = '" + diagramName + "' " +
										" and d.Package_ID in (" + packageIDList + ") ";
			return this.EAModel.getDiagramsByQuery(sqlGetOwnedDiagram);
		}
		public HashSet<UML.Classes.Kernel.Package> getNestedPackageTree(bool includeThis)
		{
			var nestedPackageTree = this.nestedPackages;
			//add this package if needed
			if (includeThis) nestedPackageTree.Add(this);
			foreach (var subPackage in this.nestedPackages) 
			{
				foreach (var subSubPackage in subPackage.getNestedPackageTree(false)) 
				{
					nestedPackageTree.Add(subSubPackage);
				}
			}
			return nestedPackageTree;
		}

		public HashSet<UML.Classes.Kernel.Element> getAllOwnedElements()
		{
			var allOwnedElements = this.ownedElements;
			foreach (var subPackage in this.getNestedPackageTree(false)) 
			{
				foreach (var element in subPackage.ownedElements) 
				{
					allOwnedElements.Add(element);
				}
			}
			return allOwnedElements;
		}

		public string getPackageIDString(ICollection<UML.Classes.Kernel.Package> packages)
		{
			var ids = packages.Select(x => ((Package)x).packageID);
			string idString = string.Join(",",ids);
			return idString;
		}
		public List<string> getPackageTreeIDs(List<string> parentIDs = null)
		{
			List<string> allPackageIDs = new List<string>();
			List<string>subPackageIDs = new List<string>();
			if (parentIDs == null)
			{
				parentIDs = new List<string>(){this.packageID.ToString()};
			}
			//add the current parentID's to the list of all ID's
			allPackageIDs.AddRange(parentIDs);
			//get the id's from the subpackages
			string parentIDString = string.Join(",",parentIDs);
			string getSubpackageSQL = "select p.Package_ID from t_package p where p.Parent_ID in ("+ parentIDString +")";
			var queryResult = this.EAModel.SQLQuery(getSubpackageSQL);
			foreach (XmlNode packageIdNode in queryResult.SelectNodes("//Package_ID")) 
			{
				subPackageIDs.Add(packageIdNode.InnerText);
			}
			//if subpackages found then go a level deeper
			if (subPackageIDs.Any())
			{
				allPackageIDs.AddRange(getPackageTreeIDs(subPackageIDs));
			}
			return allPackageIDs;
		}
		public string getPackageTreeIDString()
		{
			return string.Join(",",this.getPackageTreeIDs());
		}
		
		
	}
}
