﻿
using System;
using System.Collections.Generic;
using System.Linq;
using UML=TSF.UmlToolingFramework.UML;
using UTF_EA=TSF.UmlToolingFramework.Wrappers.EA;
using DB=DatabaseFramework;
using DB_EA = EAAddinFramework.Databases;
using EAAddinFramework.Utilities;

namespace EAAddinFramework.Databases.Transformation
{
	/// <summary>
	/// Description of EADatabaseTransformer.
	/// </summary>
	public abstract class EADatabaseTransformer:EADatabaseItemTransformer, DB.Transformation.DatabaseTransformer
	{
		private DatabaseFactory _factory;
		internal UTF_EA.Model _model;		
		internal Database _newDatabase;
		internal Database _existingDatabase;
		internal UTF_EA.Package _logicalPackage;
		protected List<EATableTransformer> _tableTransformers = new List<EATableTransformer>();

		public EADatabaseTransformer(DatabaseFactory factory, UTF_EA.Model model,NameTranslator nameTranslator):base(nameTranslator)
		{
			this._factory = factory;
			this._model = model;
		}

		public DatabaseFactory factory {
			get {return _factory;}
		}

		#region DatabaseTransformer implementation


		public UML.Extended.UMLModel model 
		{
			get {return _model;}
			set {_model = (UTF_EA.Model) value;}
		}



		public void refresh()
		{
			this._existingDatabase = null;
			this._newDatabase = null;
			this._tableTransformers.Clear();
			this.transformLogicalPackage(this.logicalPackage);
		}

		public DB.Database newDatabase 
		{
			get {return this._newDatabase;}
			set {this._newDatabase = (Database)value;}
		}

		public DB.Database existingDatabase 
		{
			get 
			{
				if (this._existingDatabase == null)
				{
					var traces = _logicalPackage.relationships.Where(x => x.stereotypes.Any(y => y.name.Equals("trace",StringComparison.InvariantCultureIgnoreCase))).Cast<UTF_EA.ConnectorWrapper>();
					foreach (var trace in traces) {
						if (trace.target.Equals(_logicalPackage)
					    && trace.source is UTF_EA.Package
					    && trace.source.stereotypes.Any(x => x.name.Equals("Database",StringComparison.InvariantCultureIgnoreCase)))
						{
							_existingDatabase = factory.createDataBase(trace.source as UTF_EA.Package);
							break;//return only one
						}
					}
				}
				return this._existingDatabase;
			}
			set {this._existingDatabase = (Database)value;}
		}

		public UML.Classes.Kernel.Package logicalPackage 
		{
			get {return this._logicalPackage;}
			set {this._logicalPackage = (UTF_EA.Package)value;}
		}
		public List<DB.Transformation.TableTransformer> tableTransformers {
			get 
			{
				return _tableTransformers.Cast<DB.Transformation.TableTransformer>().ToList();
			}
			set 
			{
				this._tableTransformers = value.Cast<EATableTransformer>().ToList();
			}
		}
		public virtual DatabaseFramework.Database transformLogicalPackage(UML.Classes.Kernel.Package logicalPackage)
		{
			this.logicalPackage = logicalPackage;
			
			createNewDatabase();
			//first create all tables
			this.createTables();
			//return the database
			return newDatabase;
		}
		/// <summary>
		/// create the initial database from the logical package
		/// </summary>
		/// <param name="ldmPackage">the logical pacakge</param>
		/// <returns>the new database</returns>
		protected abstract void createNewDatabase();

		protected virtual void createTables()
		{
			foreach (UTF_EA.Class classElement in logicalPackage.ownedElements.OfType<UTF_EA.Class>()) 
			{
				addTable(classElement);
			}
			//set table names for tables that don't have a name yet;
			nameUnnamedTables();
		}
		protected abstract void addTable(UTF_EA.Class classElement);
		
		protected abstract void nameUnnamedTables();
		
		#endregion
	}
}