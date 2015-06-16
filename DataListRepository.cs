using MD.CloudConnect.CouchBaseProvider;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MD.CloudConnect.CouchBaseProvider
{
    public class DataListRepository : RepositoryBase<DataList>
    {
        public StringBuilder _log = new StringBuilder();

        public const int MAX_SIZE = 1000;
        public DataList Information(string dataType)
        {
            return LoadInformation(dataType);
        }

        public bool PushToList(ModelBase model, int expiration = 0)
        {
            return PushToList(model.Type, new List<string>() { model.Id });
        }

        public string FlushLog()
        {
            string res = "";
            lock (_log)
            {
                res = _log.ToString();
                _log.Clear();
            }
            return res;
        }

        // NOTE : this method does not manage duplicate entry
        public bool PushToList(string dataType, List<string> keys, int expiration = 0)
        {
            int retry = 0;
            do
            {
                bool locked;
                DataList listInformation = LoadInformationWithLock(dataType, out locked);
                if (locked)
                {
                    while (locked)
                    {
                        Thread.Sleep(1000);
                        listInformation = LoadInformationWithLock(dataType, out locked);
                    }
                }
                DataList finalTest = LoadInformation(dataType);
                //if (listInformation == null)
                //{
                //    if (finalTest == null)
                //        throw new Exception("is it normal?");
                //    else
                //        retry++;
                //}
                //else
                {
                    _log.AppendLine(String.Format("[{0}] log : {1}", DateTime.UtcNow, "[LOCK] PushToList for " + dataType + " cas : " + listInformation.CasID.ToString()));

                    DataList current = null;
                    List<string> finalkeys = null;
                    List<string> tobesaved = keys;
                    bool result = false;
                    do
                    {
                        finalkeys = tobesaved.Take((int)(MAX_SIZE - listInformation.Size % MAX_SIZE)).ToList();
                        if (listInformation.Size % MAX_SIZE == 0)
                        {
                            current = new DataList()
                            {
                                Page = listInformation.Last + 1,
                                Keys = finalkeys,
                                DataType = dataType
                            };
                            if (listInformation.Size == 0)
                                listInformation.First = current.Page;
                            listInformation.Last = current.Page;
                            listInformation.Size += (uint)finalkeys.Count;
                        }
                        else
                        {
                            current = Get(String.Format("DLIST:{0}:{1}", dataType.ToUpper(), listInformation.Last));
                            if (current == null)
                                throw new Exception("[PushToList}] fail to get " + String.Format("DLIST:{0}:{1}", dataType.ToUpper(), listInformation.Last));
                            current.Keys.AddRange(finalkeys);
                            listInformation.Size += (uint)finalkeys.Count;
                        }
                        result = Save(current);
                        tobesaved = tobesaved.Skip((int)(MAX_SIZE - listInformation.Size % MAX_SIZE)).ToList();
                    } while (tobesaved.Count > 0);
                    if (result)
                    {
                        if (!SaveWithCas(listInformation, listInformation.CasID))
                        {
                            retry++;
                            _log.AppendLine(String.Format("[{0}] log : {1}", DateTime.UtcNow, "[FAIL RELEASE LOCK] PushToList for " + " cas : " + listInformation.CasID.ToString() + " retry : " + retry.ToString()));
                        }
                        _log.AppendLine(String.Format("[{0}] log : {1}", DateTime.UtcNow, "[RELEASE LOCK] PushToList for " + dataType.ToUpper() + " cas : " + listInformation.CasID.ToString()));
                        return true;
                    }
                    else
                    {
                        UnLock(listInformation);
                    }
                }
            } while (retry < 20);
            if (retry >= 20)
                throw new Exception("[PushToList}] 20 retry to save " + keys.Count() + dataType);

            return false;
        }

        public int Size(string dataType)
        {
            DataList listInformation = LoadInformation(dataType);
            return (int)listInformation.Size;
        }

        public DataList GetPage(string dataType, uint page = 0)
        {
            DataList listInformation = LoadInformation(dataType);
            if (listInformation == null)
                return null;
            DataList current = null;
            if (listInformation.Size > 0 && listInformation.First + page <= listInformation.Last)
            {
                current = Get(String.Format("DLIST:{0}:{1}", dataType.ToUpper(), listInformation.First + page));

                if (current == null)
                    throw new Exception("[GetPage] fail to get " + String.Format("DLIST:{0}:{1}", dataType.ToUpper(), listInformation.Last));
            }
            else
                return null;
            return current;
        }

        public bool UpdateDataList(List<DataList> pages, List<string> keys_removed)
        {
            int retry = 0;
            do
            {
                if (keys_removed.Count > 0 && pages.Count > 0)
                {
                    string dataType = pages.First().DataType;
                    bool canRemove = true;

                    bool locked;
                    DataList listInformation = LoadInformationWithLock(dataType, out locked);
                    if (locked)
                    {
                        while (locked || listInformation == null)
                        {
                            Thread.Sleep(1000);
                            listInformation = LoadInformationWithLock(dataType, out locked);
                        }
                    }
                    if (listInformation == null)
                        retry++;
                    else
                    {
                        _log.AppendLine(String.Format("[{0}] log : {1}", DateTime.UtcNow, ("[LOCK] UpdateDataList for " + dataType + " cas : " + listInformation.CasID.ToString())));
                        foreach (DataList data in pages)
                        {
                            int count = data.Keys.RemoveAll(x => keys_removed.Contains(x));
                            listInformation.Size -= (uint)count;
                            if (listInformation.Size < 0)
                                throw new Exception("[UpdateDataList] Overflow");

                            if (data.Keys.Count == 0 && canRemove)
                            {
                                if (listInformation.First == listInformation.Last)
                                {
                                    listInformation.First = 0;
                                    listInformation.Last = 0;
                                    listInformation.Size = 0;
                                }
                                else
                                    listInformation.First = data.Page + 1;
                                SaveWithExpireTime(data, 1);
                            }
                            else
                            {
                                canRemove = false;
                                Save(data);
                            }
                        }
                        if (!SaveWithCas(listInformation, listInformation.CasID))
                        {
                            retry++;
                            _log.AppendLine(String.Format("[{0}] log : {1}", DateTime.UtcNow, "[FAIL RELEASE LOCK] PushToList for " + " cas : " + listInformation.CasID.ToString() + " retry : " + retry.ToString()));
                        }

                        _log.AppendLine(String.Format("[{0}] log : {1}", DateTime.UtcNow, "[RELEASE LOCK] UpdateDataList for " + dataType + " cas : " + listInformation.CasID.ToString()));
                        return true;
                    }
                }
            } while (retry < 20);
            if (retry >= 20)
                throw new Exception("[UpdateDataList}]20 retry to save " + pages.First().DataType);
            return true;
        }

        private DataList LoadInformationWithLock(string dataType, out bool locked)
        {
            DataList listInformation = GetWithLock(String.Format("DLIST:{0}:0", dataType.ToUpper()), out locked);
            if (locked)
            {
                _log.AppendLine(String.Format("[{0}] log : {1}", DateTime.UtcNow, "[FAIL LOCK] for" + dataType));

            }
            else if (listInformation == null)
                listInformation = new DataList()
                {
                    Page = 0,
                    First = 0,
                    Last = 0,
                    Size = 0,
                    DataType = dataType
                };
            return listInformation;
        }

        private DataList LoadInformation(string dataType)
        {
            DataList listInformation = listInformation = Get(String.Format("DLIST:{0}:0", dataType.ToUpper()));
            //if (listInformation == null)
            //{

            //    listInformation = new DataList()
            //    {
            //        Page = 0,
            //        First = 0,
            //        Last = 0,
            //        Size = 0,
            //        DataType = dataType
            //    };
            //}
            return listInformation;
        }


        public override string BuildKey(DataList model)
        {
            return String.Format("DLIST:{0}:{1}", model.DataType.ToUpper(), model.Page);
        }
    }
}
