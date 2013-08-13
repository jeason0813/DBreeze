﻿/* 
  Copyright (C) 2012 dbreeze.tiesky.com / Alex Solovyov / Ivars Sudmalis.
  It's a free software for those, who thinks that it should be free.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.IO;

using DBreeze.Transactions;
using DBreeze.Exceptions;

//under DBreeze main namespace we hold Schema and Engine.

namespace DBreeze
{
    /// <summary>
    /// Main DBreeze Database class.
    /// </summary>
    public class DBreezeEngine:IDisposable
    {
        #region "Version Number"
        /// <summary>
        /// DBreeze version number
        /// </summary>        
        public static string Version = "01.060.20130813";
        #endregion

   
        //later can be swapped on Configuration.DBreezeDataFolderName;
        internal string MainFolder = String.Empty;
        internal Scheme DBreezeSchema = null;
        internal TransactionsCoordinator _transactionsCoordinator = null;
        internal bool DBisOperable = true;
        internal TransactionsJournal _transactionsJournal = null;
        internal TransactionTablesLocker _transactionTablesLocker = null;
        bool disposed = false;

        /// <summary>
        /// Dbreeze Configuration.
        /// For now BackupPlan is included.
        /// Later can be added special settings for each entity defined by string pattern.
        /// </summary>
        internal DBreezeConfiguration Configuration = new DBreezeConfiguration();
        
        /// <summary>
        /// Dbreeze instantiator
        /// </summary>
        /// <param name="dbreezeConfiguration"></param>
        public DBreezeEngine(DBreezeConfiguration dbreezeConfiguration)
        {
            if (Configuration != null)
                Configuration = dbreezeConfiguration;
            
            //Setting up in backup DbreezeFolderName, there must be found at least TransJournal and Scheme.
            //Configuration.Backup.SynchronizeBackup has more information
            if (Configuration.Backup.IsActive)
            {
                Configuration.Backup.DBreezeFolderName = Configuration.DBreezeDataFolderName;

                ////Running backup synchronization
                //Configuration.Backup.SynchronizeBackup();
            }

            MainFolder = Configuration.DBreezeDataFolderName;

            InitDb();

            //Console.WriteLine("DBreeze notification: Don't forget in the dispose function of your DLL or main application thread");
            //Console.WriteLine("                      to dispose DBreeze engine:  if(_engine != null) _engine.Dispose(); ");
            //Console.WriteLine("                      to get graceful finilization of all working threads! ");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="DBreezeDataFolderName"></param>
        public DBreezeEngine(string DBreezeDataFolderName)
        {
            MainFolder = DBreezeDataFolderName;
            Configuration.DBreezeDataFolderName = DBreezeDataFolderName;

            InitDb();

            //Console.WriteLine("DBreeze notification: Don't forget in the dispose function of your DLL or main application thread");
            //Console.WriteLine("                      to dispose DBreeze engine:  if(_engine != null) _engine.Dispose(); ");
            //Console.WriteLine("                      to get graceful finilization of all working threads! ");
        }

        private void InitDb()
        {
            //trying to check and create folder
            

            try
            {

                if (Configuration.Storage == DBreezeConfiguration.eStorage.DISK)
                {
                    DirectoryInfo di = new DirectoryInfo(MainFolder);

                    if (!di.Exists)
                        di.Create();
                }               

                //trying to open schema file
                DBreezeSchema = new Scheme(this);

                //Initializing Transactions Coordinator
                _transactionsCoordinator = new TransactionsCoordinator(this);

                //Initializing transactions Journal, may be later move journal into transactionsCoordinator
                //We must create journal after Schema, for getting path to rollback files
                _transactionsJournal = new TransactionsJournal(this);

                //Initializes transaction locker, who can help block tables of writing and reading threads
                _transactionTablesLocker = new TransactionTablesLocker();
            }
            catch (Exception ex)
            {
                DBisOperable = false;
                throw DBreezeException.Throw(DBreezeException.eDBreezeExceptions.CREATE_DB_FOLDER_FAILED, ex);
            }

            
        }

        public void Dispose()
        {
            if (disposed)
                return;

            DBisOperable = false;
            disposed = true;

            //Disposing all transactions
            _transactionsCoordinator.StopEngine();

            //Disposing Schema
            DBreezeSchema.Dispose();

            //Disposing Trnsactional Journal, may be later move journal into transactionsCoordinator
            _transactionsJournal.Dispose();

           //Disposing Configuration
            Configuration.Dispose();
            
            //MUST BE IN THE END OF ALL.Disposing transaction locker
            _transactionTablesLocker.Dispose();
        }


        /// <summary>
        /// Returns transaction object.
        /// </summary>
        /// <returns></returns>
        public Transaction GetTransaction()
        {
            if (!DBisOperable)
                throw DBreezeException.Throw(DBreezeException.eDBreezeExceptions.DB_IS_NOT_OPERABLE);              

            //User receives new transaction from the engine
            return this._transactionsCoordinator.GetTransaction(0, eTransactionTablesLockTypes.SHARED);

        }

        /// <summary>
        /// Returns transaction object.
        /// </summary>
        /// <param name="tablesLockType">
        /// <para>SHARED: threads can use listed tables in parallel. Must be used together with tran.SynchronizeTables command, if necessary.</para>
        /// <para>EXCLUSIVE: if other threads use listed tables for reading or writing, current thread will be in a waiting queue.</para>
        /// </param>
        /// <param name="tables"></param>
        /// <returns>Returns transaction object</returns>
        public Transaction GetTransaction(eTransactionTablesLockTypes tablesLockType, params string[] tables)
        {
            if (!DBisOperable)
                throw DBreezeException.Throw(DBreezeException.eDBreezeExceptions.DB_IS_NOT_OPERABLE);

            //User receives new transaction from the engine
            return this._transactionsCoordinator.GetTransaction(1, tablesLockType, tables);

        }

       
        /// <summary>
        /// Returns DBreeze schema object
        /// </summary>
        public Scheme Scheme
        {
            get
            {
                return this.DBreezeSchema;
            }
        }

    }//end of class

}