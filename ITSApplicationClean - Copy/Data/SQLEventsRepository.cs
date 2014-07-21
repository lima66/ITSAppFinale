﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ObjectModel;
using System.Configuration;
using System.Data.SqlClient;

namespace Data
{
    public class SQLEventsRepository : IRepository<Event>
    {
        string connectionString;
        public SQLEventsRepository()
            : this("virtualMachineCS")
        {
        }
        public SQLEventsRepository(string connectionStringName)
        {
            var cs = ConfigurationManager.ConnectionStrings[connectionStringName];
            if (cs == null)
                throw new ApplicationException(string.Format("ConnectionString '{0}' not found", connectionStringName));
            connectionString = cs.ConnectionString;
        }
        public IEnumerable<Event> GetAll()
        {
            List<Event> events = new List<Event>();

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                string query = @"SELECT [ID]
                                ,[Data]
                                ,[Titolo]
                                ,[Testo]
                                ,[UrlImmagine]
                                FROM [dbo].[Eventi]";

                using (var command = new SqlCommand(query, connection))
                {
                    using (
                        SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Event event_obj = new Event();
                            event_obj.Id = reader.GetValue<int>("ID");
                            event_obj.DataPubblicazione = reader.GetValue<string>("Data");
                            event_obj.Titolo = reader.GetValue<string>("Titolo");
                            event_obj.Testo = reader.GetValue<string>("Testo");
                            event_obj.UrlFoto = reader.GetValue<string>("UrlImmagine");

                            events.Add(event_obj);
                        }
                    }
                }
            }

            return events;
        }
        public Event Get(int id)
        {
            string query = @"SELECT 
                                ID,
                                Data,
                                Titolo,
                                Testo,
                                UrlImmagine
                                FROM Eventi
                                WHERE ID = @id";

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.Add(new SqlParameter("@id", id));

                    {
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                Event event_obj = new Event();
                                event_obj.Id = reader.GetValue<int>("ID");
                                event_obj.DataPubblicazione = reader.GetValue<string>("Data");
                                event_obj.Titolo = reader.GetValue<string>("Titolo");
                                event_obj.Testo = reader.GetValue<string>("Testo");
                                event_obj.UrlFoto = reader.GetValue<string>("UrlImmagine");

                                return event_obj;
                            }
                            return null;
                        }
                    }
                }
            }
        }
        public int Post(Event eventObj)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                string query = @"INSERT INTO [dbo].[Eventi]
                                ([Data]
                                ,[DataEvento]
                                ,[Titolo]
                                ,[Testo]
                                ,[UrlImmagine])
                                OUTPUT INSERTED.ID
                                VALUES
                                (@Data
                                ,@DataEvento
                                ,@Titolo
                                ,@Testo
                                ,@UrlImmagine);";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.Add(new SqlParameter("@Data", Date.getDate()));
                    command.Parameters.Add(new SqlParameter("@DataEvento", "01/01/01"));
                    command.Parameters.Add(new SqlParameter("@Titolo", eventObj.Titolo));
                    command.Parameters.Add(new SqlParameter("@Testo", eventObj.Testo));
                    command.Parameters.Add(new SqlParameter("@UrlImmagine", eventObj.UrlFoto));

                    int lastID = (int)command.ExecuteScalar();

                    connection.Close();

                    string key = "Event_id_" + lastID;

                    RedisNotificationRepository.Insert(key);

                    return lastID;
                }
            }
        }
        public bool Put(Event event_obj)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                string query = @"UPDATE [dbo].[Eventi]
                                    SET [Data] = @Data
                                       ,[DataEvento] = @DataEvento
                                       ,[Titolo] = @Titolo
                                       ,[Testo] = @Testo
                                       ,[UrlImmagine] = @[UrlImmagine]
                                 WHERE ID = @Id";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.Add(SqlHelper.CreateParameter("Id", event_obj.Id));
                    command.Parameters.Add(SqlHelper.CreateParameter("Data", event_obj.DataPubblicazione));
                    command.Parameters.Add(SqlHelper.CreateParameter("DataEvento", "01/01/01"));
                    command.Parameters.Add(SqlHelper.CreateParameter("Titolo", event_obj.Titolo));
                    command.Parameters.Add(SqlHelper.CreateParameter("Testo", event_obj.Testo));
                    command.Parameters.Add(SqlHelper.CreateParameter("UrlImmagine", event_obj.UrlFoto));

                    int affectedRows = command.ExecuteNonQuery();

                    if(affectedRows == 1)
                    {
                        return true;
                    }
                    return false;
                }
            }
        }
        public void Delete(int id)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                string query = @"DELETE FROM [dbo].[Eventi]
                                WHERE ID = @id";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.Add(new SqlParameter("@id", id));

                    int count = command.ExecuteNonQuery();
                }
                connection.Close();

                RedisNotificationRepository.Delete("Event_id_" + id);
            }
        }
        public IEnumerable<Event> Search(string keyWord)
        {
            List<Event> events = new List<Event>();

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                string query = @"SELECT [ID]
                                ,[Data]
                                ,[Titolo]
                                ,[Testo]
                                ,[UrlImmagine]
                                FROM [dbo].[Eventi]
                                WHERE (Titolo LIKE '%" + keyWord + "%') OR (Testo LIKE '%" + keyWord + "%')";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.Add(new SqlParameter("@Keyword", keyWord));

                    using (
                        SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Event event_obj = new Event();
                            event_obj.Id = reader.GetValue<int>("ID");
                            event_obj.DataPubblicazione = reader.GetValue<string>("Data");
                            event_obj.Titolo = reader.GetValue<string>("Titolo");
                            event_obj.Testo = reader.GetValue<string>("Testo");
                            event_obj.UrlFoto = reader.GetValue<string>("UrlImmagine");

                            events.Add(event_obj);
                        }
                    }
                }
            }

            return events;
        }
    }
}