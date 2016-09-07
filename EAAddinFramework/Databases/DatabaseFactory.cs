﻿
using System;
using System.Collections.Generic;
using TSF.UmlToolingFramework.Wrappers.EA;
using DB=DatabaseFramework;
using System.Linq;
using UML = TSF.UmlToolingFramework.UML;

namespace EAAddinFramework.Databases
{
	/// <summary>
	/// Description of DatabaseFactory.
	/// </summary>
	public class DatabaseFactory:DB.DataBaseFactory
	{
		private string _type;
		private Model _model;
		public string type 
		{
			get { return _type;}
		}

		public string databaseName 
		{
			get 
			{
				return type;
			}
		}

		private Dictionary<string, BaseDataType> _baseDataTypes;
		public List<DB.BaseDataType> baseDataTypes
		{
			get
			{
				List<DB.BaseDataType> datypes = new List<DB.BaseDataType>();
				foreach (var baseDatatype in _baseDataTypes.Values) 
				{
					datypes.Add(baseDatatype);
				}
				return datypes;
			}
		}
		/// <summary>
		/// adds a factory to the list of possible database factories
		/// </summary>
		/// <param name="type">the type of the factory</param>
		/// <param name="datatypes">the base datatypes for this factory</param>
		public static void addFactory(string type, Model model)
		{
			if (!factories.ContainsKey(type))
			{
				DatabaseFactory factory = new DatabaseFactory(type, model);
				factories.Add(type, factory);
			}
		}
		private static Dictionary<string, DatabaseFactory> factories = new Dictionary<string, DatabaseFactory>();
		public static DatabaseFactory getFactory(string type)
		{
			if (factories.ContainsKey(type))
			{
				return factories[type];
			}
			return null;
		}
		public UML.Extended.UMLFactory modelFactory
		{
			get
			{
				if (this._model != null) return (Factory)this._model.factory;
				else return null;
			}
		}
		public Factory _modelFactory
		{
			get
			{
				return modelFactory as Factory;
			}
		}
		private DatabaseFactory(string type, Model model)
		{
			this._type = type;
			this._model = model;
			this._baseDataTypes = getBaseDataTypes(type,model);
		}
		public Dictionary<string, BaseDataType> getBaseDataTypes(string databaseType, Model model)
		{
			Dictionary<string, BaseDataType> datatypes = new Dictionary<string, BaseDataType>();
			foreach (global::EA.Datatype eaDataType in model.getWrappedModel().Datatypes)
			{
				if (eaDataType.Product.Equals(databaseType,StringComparison.InvariantCultureIgnoreCase)
				    && eaDataType.Type == "DDL")
				{
					var datatype = new BaseDataType(eaDataType);
					datatypes.Add(datatype.name, datatype);
				}
			}
			return datatypes;
		}
		public Database createDataBase(Package package)
		{
			return new Database(package, this);
		}
		public Database createDatabase(string name)
		{
			return new Database(name, this);
		}
		public DataType createDataType(string compositeName)
		{
			string baseTypeName;
			int length = 0;
			int precision = 0;
			if (compositeName.Contains("("))
			{
				baseTypeName = compositeName.Substring(0,compositeName.IndexOf("("));
				string scaleString = compositeName.Substring(compositeName.IndexOf("(") +1);
				if (scaleString.Contains(","))
				{
					int.TryParse(scaleString.Substring(0,scaleString.IndexOf(",")),out length );
					string precisionString = scaleString.Substring(scaleString.IndexOf(",") +1 ,scaleString.Length-scaleString.IndexOf(",") -2);
					int.TryParse(precisionString,out precision);
				}
				else
				{
					int.TryParse(scaleString.Substring(0, scaleString.Length -1),out length);
				}
			}
			else
			{
				baseTypeName = compositeName;
			}
			BaseDataType basetype = this.baseDataTypes.FirstOrDefault(x => x.name == baseTypeName) as BaseDataType;
			if (basetype != null)
			{
				return new DataType(basetype,length,precision);
			}
			else
			{
				return null;
			}
			 
		}
	}
}