﻿using Dapper;
using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection.PortableExecutable;
using System.Text.RegularExpressions;
using static System.Runtime.InteropServices.JavaScript.JSType;


namespace searchdata.Model
{
    public class AIOTService
    {
        private readonly string connectionString;
        public AIOTService(IConfiguration configuration)
        {
            connectionString = configuration.GetConnectionString("AIOT");

        }
        string _sql = "";
        public dynamic getOneLineData(string startTime, string endTime, string item, string product, string line, string? reporttype)
        {
            List<machineData> datalist = new List<machineData>();
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                switch (reporttype)
                {
                    case "date":
                        _sql = $"SELECT * FROM [AIOT].[dbo].[Line_MachineData] WHERE Item = @item AND Product = @product AND Date BETWEEN @startTime AND @endTime AND Line = @line Order by Date";
                        datalist.AddRange(connection.Query<machineData>(_sql, new { startTime, endTime, item, product, line }).ToList());
                        break;
                    case "week":
                        //2023-11、2024-03
                        var listWeek = new List<Date>();
                        var startWeekYear = Convert.ToInt32(startTime.Split('-')[0]);
                        var endWeekYear = Convert.ToInt32(endTime.Split('-')[0]);
                        var countWeekYear = endWeekYear - startWeekYear;
                        var startWeek = Convert.ToInt32(startTime.Split("-")[1].Split("W")[1]);
                        //如果年份不一樣就乘上差異加到endWeek
                        var endWeek = countWeekYear > 0 ? Convert.ToInt32(endTime.Split("-")[1].Split("W")[1]) + (countWeekYear * 52) : Convert.ToInt32(endTime.Split("-")[1].Split("W")[1]);
                        var countWeek = endWeek - startWeek;
                        for (int i = 0; i <= countWeek; i++)
                        {

                            getDateforWeek(startWeekYear, startWeek, listWeek);
                            startWeek += 1;
                            if (startWeekYear < endWeekYear && startWeek > 52)
                            {
                                startWeekYear += 1;
                                startWeek = 1;
                            }
                        }
                        getOneLineWeekAndMonthReport(item, product, line, datalist, connection, listWeek);
                        break;

                    case "month":
                        //2023-12、2024-3
                        var listMonth = new List<Date>();
                        var startMonthYear = Convert.ToInt32(startTime.Split('-')[0]);
                        var endtMonthYear = Convert.ToInt32(endTime.Split('-')[0]);
                        var countMonthYear = endtMonthYear - startMonthYear;
                        var startMonth = Convert.ToInt32(startTime.Split("-")[1]);
                        //如果年份不一樣就乘上差異加到endWeek
                        var endMonth = countMonthYear > 0 ? Convert.ToInt32(endTime.Split("-")[1]) + (countMonthYear * 12) : Convert.ToInt32(endTime.Split("-")[1]);
                        var countMonth = endMonth - startMonth;
                        for (int i = 0; i <= countMonth; i++)
                        {
                            getDatefoMonth(startMonthYear, startMonth, listMonth);
                            startMonth += 1;
                            if (startMonthYear < endtMonthYear && startMonth > 12)
                            {
                                startMonthYear += 1;
                                startMonth = 1;
                            }
                        }
                        getOneLineWeekAndMonthReport(item, product, line, datalist, connection, listMonth);
                        break;
                }
                if (datalist.Count > 0)
                {
                    machineData machineData = new machineData();
                    machineData.Date = null;
                    machineData.ACT = Math.Round((double)datalist.Sum(x => x.ACT), 2);
                    machineData.ACTH = Math.Round((double)datalist.Sum(x => x.ACTH), 2);
                    machineData.AO = datalist.Sum(x => Convert.ToInt32(x.AO)).ToString();
                    machineData.CAPU = Math.Round((double)datalist.Average(x => x.CAPU), 2);
                    machineData.ADR = Math.Round((double)datalist.Average(x => x.ADR), 2);
                    machineData.Availability = Math.Round((double)datalist.Average(x => x.Availability), 2) > 100 ? 99 : Math.Round((double)datalist.Average(x => x.Availability), 2);
                    machineData.YieId = Math.Round((double)datalist.Average(x => x.YieId), 2) > 100 ? 99 : Math.Round((double)datalist.Average(x => x.YieId), 2);
                    machineData.Performance = Math.Round((double)(machineData.ACTH / machineData.ACT) * 100, 2) > 100 ? 99 : Math.Round((double)(machineData.ACTH / machineData.ACT) * 100, 2);
                    machineData.OEE = Math.Round((double)datalist.Average(x => x.OEE), 2) > 100 ? 99 : Math.Round((double)datalist.Average(x => x.OEE), 2);
                    machineData.NonTime = Math.Round((double)datalist.Sum(x => x.NonTime), 2);
                    machineData.StopRunTime = Math.Round((double)datalist.Sum(x => x.StopRunTime), 2);
                    machineData.AllNGS = datalist.Sum(x => x.AllNGS);
                    datalist.Add(machineData);
                }
                return datalist;
            }
        }
        public dynamic getOneLineNonTimeData(string startTime, string endTime, string item, string product, string line, string device)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                _sql = @"SELECT M.DeviceName, ISNULL(LMN.Description,'6S') AS Description,LMN.Date,LMN.StartTime,LMN.EndTime,ROUND(LMN.SumTime,2) AS SumTime ";
                _sql += " FROM (SELECT * FROM [AIOT].[dbo].[Line_MachineNonTime] WHERE Date BETWEEN @startTime AND @endTime) AS LMN";
                _sql += " LEFT JOIN [AIOT].[dbo].[Machine] AS M ON M.IODviceName = LMN.DeviceName ";
                _sql += " LEFT JOIN [AIOT].[dbo].[ProductProductionLines] AS PPL ON PPL.id = M.ProductProductionLinesID ";
                _sql += " LEFT JOIN [AIOT].[dbo].[ProductLine] AS PL ON PL.LineID = PPL.LineID ";
                _sql += " LEFT JOIN [AIOT].[dbo].[Product] AS P ON PPL.ProductID = P.ProductID ";
                _sql += " LEFT JOIN [AIOT].[dbo].[Item] AS I ON P.ItemID = I.ItemID ";
                _sql += " WHERE I.ItemName =  @item AND P.ProductName = @product AND PL.LineName = @line ";
                _sql += device == "All" ? " " : " AND M.DeviceName = @device";
                _sql += " ORDER BY LMN.DeviceName,Date,LMN.StartTime";
                var datalist = connection.Query<OneLineNonTimeTable>(_sql, new { startTime, endTime, item, product, line, device });
                return datalist;
            }

        }
        private void getOneLineWeekAndMonthReport(string item, string product, string line, List<machineData> datalist, SqlConnection connection, List<Date> listWeek)
        {
            foreach (var data in listWeek)
            {
                _sql = "SELECT Item,Product,Alloted,Folor,Line";
                _sql += " ,ROUND(SUM(CAST(ETC AS FLOAT)),2) AS ETC";
                _sql += " ,ROUND(SUM(CAST(PT AS FLOAT)),2) AS PT";
                _sql += " ,ROUND(SUM(CAST(ACT AS FLOAT)),2) AS ACT";
                _sql += " ,ROUND(SUM(CAST(ACTH AS FLOAT)),2) AS ACTH";
                _sql += " ,SUM(CAST(AO AS FLOAT)) AS AO";
                _sql += " ,ROUND(AVG(CAST(CAPU AS FLOAT)),2) AS CAPU ";
                _sql += " ,ROUND(AVG(CAST(ADR AS FLOAT)),2) AS ADR";
                _sql += " ,ROUND(AVG(CAST(Performance AS FLOAT)),2) AS Performance";
                _sql += " ,ROUND(AVG(CAST(YieId AS FLOAT)),2) AS YieId";
                _sql += " ,ROUND(AVG(CAST(Availability AS FLOAT)),2) AS Availability";
                _sql += " ,ROUND(AVG(CAST(OEE AS FLOAT)),2) AS OEE";
                _sql += " ,ROUND(SUM(CAST(NonTime AS FLOAT)),2) AS NonTime";
                _sql += " ,ROUND(SUM(CAST(StopRunTime AS FLOAT)),2) AS StopRunTime";
                _sql += " ,SUM(CAST(AllNGS AS int)) AS AllNGS";
                _sql += " FROM[AIOT].[dbo].[Line_MachineData] ";
                _sql += " WHERE Item = @item AND Product = @product AND Date BETWEEN @startDate AND @endDate AND Line = @line";
                _sql += " GROUP BY Item,Product,Alloted,Folor,Line";
                var tempdata = connection.Query<machineData>(_sql, new { data.startDate, data.endDate, item, product, line }).FirstOrDefault();
                if (tempdata != null)
                {
                    tempdata.Date = string.IsNullOrEmpty(data.week) ? data.year + "_" + data.month : data.year + "_" + data.week;
                    datalist.Add(tempdata);
                }
            }
        }
        private void getOneLineERROrderWeekAndMonthReport(string item, string product, string line, string type, string device, List<OneLineERROrder> datalist, SqlConnection connection, List<Date> listWeek)
        {
            foreach (var data in listWeek)
            {
                _sql = @" WITH LM_CTE AS (SELECT M.IODviceName AS DeviceName, PL.[LineName] AS ProductLine, LM.Date, LM.Time, LM.Type, LM.Name, LM.Count ";
                _sql += $" FROM (SELECT * FROM [AIOT].[dbo].[Line_MachineERRData] WHERE Date BETWEEN '{data.startDate}' AND '{data.endDate}' AND Type = '{type}') AS LM";
                _sql += " LEFT JOIN [AIOT].[dbo].[Machine] AS M ON M.IODviceName = LM.DeviceName";
                _sql += " LEFT JOIN [AIOT].[dbo].[ProductProductionLines] AS PPL ON PPL.id = M.ProductProductionLinesID";
                _sql += " LEFT JOIN [AIOT].[dbo].[ProductLine] AS PL ON PL.LineID = PPL.LineID";
                _sql += " LEFT JOIN [AIOT].[dbo].[Product] AS P ON PPL.ProductID = P.ProductID";
                _sql += " LEFT JOIN [AIOT].[dbo].[Item] AS I ON P.ItemID = I.ItemID";
                _sql += $"  WHERE I.ItemName = '{item}' AND P.ProductName = '{product}' AND PL.LineName = '{line}' ";
                _sql += device == "All" ? " )" : $" AND M.DeviceName = '{device}')";
                _sql += " SELECT DeviceName,ProductLine,REPLACE(REPLACE(REPLACE(REPLACE(Time, CHAR(13) + CHAR(10), CHAR(13)), ' ',' '), CHAR(10)+CHAR(10), CHAR(10)),CHAR(10), '\n') AS Time,Date,Type,Name,Count";
                _sql += " FROM LM_CTE";
                _sql += " WHERE Count <> 0";
                _sql += " ORDER BY Date DESC, CAST(Count AS INT) DESC ";
                var tempdata = connection.Query<OneLineERROrder>(_sql).ToList();
                if (tempdata != null)
                {
                    foreach (var temp in tempdata)
                    {
                        temp.Date = string.IsNullOrEmpty(data.week) ? data.year + "_" + data.month : data.year + "_" + data.week;
                        datalist.Add(temp);
                    }
                }
            }
        }

        private void getMoreLineWeekAndMonthReport(string item, string product, List<string> arrLine, string selectType, List<BackMoreLineData> datalist, SqlConnection connection, List<Date> listWeek, string _sql)
        {
            foreach (var data in listWeek)
            {
                var tempdata = connection.Query<BackMoreLineData>(_sql, new { data.startDate, data.endDate, item, product, arrLine, selectType }).ToList();
                if (tempdata != null)
                {
                    foreach (var tempitem in tempdata)
                    {
                        tempitem.Date = string.IsNullOrEmpty(data.week) ? data.year + "_" + data.month : data.year + "_" + data.week;
                        datalist.Add(tempitem);
                    }
                }
            }
        }

        public dynamic getMoreLineData(FrontMoreLineData frontMoreLineData)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                string forlindata = "";
                string forERRdata = "";
                var datalist = new List<BackMoreLineData>();
                forlindata = $"SELECT Line,Date,{frontMoreLineData.selectType} as Value ";
                forlindata += " FROM [AIOT].[dbo].[Line_MachineData] ";
                forlindata += " WHERE Item = @item AND Product = @product AND Date BETWEEN @startDate AND @endDate AND Line IN @arrLine Order by Date";


                forERRdata = "SELECT Line, Date, ROUND((ERRCOUNT / MACHINE / ETC),2) AS Value ";
                forERRdata += " FROM (SELECT PL.LineName AS Line,LMD.Date AS Date ";
                forERRdata += ", (SELECT COUNT(IODviceName) FROM [AIOT].[dbo].[Machine] AS M";
                forERRdata += " LEFT JOIN [AIOT].[dbo].[ProductProductionLines] AS PPL ON PPL.id = M.ProductProductionLinesID ";
                forERRdata += " LEFT JOIN [AIOT].[dbo].[ProductLine] AS PLL ON PLL.LineID = PPL.LineID ";
                forERRdata += " LEFT JOIN [AIOT].[dbo].[Product] AS P ON PPL.ProductID = P.ProductID ";
                forERRdata += " LEFT JOIN [AIOT].[dbo].[Item] AS I ON P.ItemID = I.ItemID ";
                forERRdata += " WHERE PLL.LineName = PL.LineName AND I.ItemName = @Item AND P.ProductName = @Product) AS MACHINE ";
                forERRdata += ", (SELECT SUM(CAST(ETC AS float)) FROM[AIOT].[dbo].[Line_MachineData] WHERE Line = PL.LineName AND Item = @Item AND Product = @Product AND Date = LMD.Date) AS ETC ";
                forERRdata += ", SUM(CAST(Count AS float)) AS ERRCOUNT ";
                forERRdata += " FROM(SELECT * FROM[AIOT].[dbo].[Line_MachineERRData] WHERE Date BETWEEN @startDate AND @endDate AND Type = @selectType) AS LMD ";
                forERRdata += " LEFT JOIN [AIOT].[dbo].[Machine] AS M ON M.IODviceName = LMD.DeviceName ";
                forERRdata += " LEFT JOIN [AIOT].[dbo].[ProductProductionLines] AS PPL ON PPL.id = M.ProductProductionLinesID ";
                forERRdata += " LEFT JOIN [AIOT].[dbo].[ProductLine] AS PL ON PL.LineID = PPL.LineID ";
                forERRdata += " LEFT JOIN [AIOT].[dbo].[Product] AS P ON PPL.ProductID = P.ProductID ";
                forERRdata += " LEFT JOIN [AIOT].[dbo].[Item] AS I ON P.ItemID = I.ItemID ";
                forERRdata += " WHERE PL.LineName IN @arrLine AND I.ItemName = @Item AND P.ProductName = @Product ";
                forERRdata += " GROUP BY PL.LineName, LMD.Date) AS REPORT  ORDER BY Date";
                //判斷Type
                _sql = frontMoreLineData.selectType != "ERR" ? forlindata : forERRdata;

                switch (frontMoreLineData.reporttype)
                {
                    case "date":
                        datalist.AddRange(connection.Query<BackMoreLineData>(_sql, frontMoreLineData).ToList());
                        if (datalist.Count == 0)
                        {
                            return datalist;
                        }
                        addLineforDate(datalist);
                        break;
                    case "week":
                        var listWeek = new List<Date>();
                        var startWeekYear = Convert.ToInt32(frontMoreLineData.startDate.Split('-')[0]);
                        var endWeekYear = Convert.ToInt32(frontMoreLineData.endDate.Split('-')[0]);
                        var countWeekYear = endWeekYear - startWeekYear;
                        var startWeek = Convert.ToInt32(frontMoreLineData.startDate.Split("-")[1].Split("W")[1]);
                        //如果年份不一樣就乘上差異加到endWeek
                        var endWeek = countWeekYear > 0 ? Convert.ToInt32(frontMoreLineData.endDate.Split("-")[1].Split("W")[1]) + (countWeekYear * 52) : Convert.ToInt32(frontMoreLineData.endDate.Split("-")[1].Split("W")[1]);
                        var countWeek = endWeek - startWeek;
                        for (int i = 0; i <= countWeek; i++)
                        {

                            getDateforWeek(startWeekYear, startWeek, listWeek);
                            startWeek += 1;
                            if (startWeekYear < endWeekYear && startWeek > 52)
                            {
                                startWeekYear += 1;
                                startWeek = 1;
                            }
                        }
                        getMoreLineWeekAndMonthReport(frontMoreLineData.item, frontMoreLineData.product, frontMoreLineData.arrLine, frontMoreLineData.selectType, datalist, connection, listWeek, _sql);
                        if (datalist.Count == 0)
                        {
                            return datalist;
                        }
                        addLineforDate(datalist);
                        break;
                    case "month":

                        var listMonth = new List<Date>();
                        var startMonthYear = Convert.ToInt32(frontMoreLineData.startDate.Split('-')[0]);
                        var endMonthYear = Convert.ToInt32(frontMoreLineData.endDate.Split('-')[0]);
                        var countMonthYear = endMonthYear - startMonthYear;
                        var startMonth = Convert.ToInt32(frontMoreLineData.startDate.Split("-")[1]);
                        //如果年份不一樣就乘上差異加到endWeek
                        var endMonth = countMonthYear > 0 ? Convert.ToInt32(frontMoreLineData.endDate.Split("-")[1]) + (countMonthYear * 12) : Convert.ToInt32(frontMoreLineData.endDate.Split("-")[1]);
                        var countMonth = endMonth - startMonth;
                        for (int i = 0; i <= countMonth; i++)
                        {

                            getDatefoMonth(startMonthYear, startMonth, listMonth);
                            startMonth += 1;
                            if (startMonthYear < endMonthYear && startMonth > 12)
                            {
                                startMonthYear += 1;
                                startMonth = 1;
                            }
                        }
                        getMoreLineWeekAndMonthReport(frontMoreLineData.item, frontMoreLineData.product, frontMoreLineData.arrLine, frontMoreLineData.selectType, datalist, connection, listMonth, _sql);
                        if (datalist.Count == 0)
                        {
                            return datalist;
                        }
                        addLineforDate(datalist);
                        break;

                }
                var tempWeekList = datalist.OrderBy(x => x.Date).ThenBy(x => x.Line).GroupBy(x => new { x.Line, x.Date }).Select(x =>
                {
                    var arrValue = Math.Round(x.Average(x => Convert.ToDouble(x.Value)), 2);
                    return new
                    {
                        x.Key.Line,
                        x.Key.Date,
                        arrValue,
                    };
                }).ToList();

                var reportList = tempWeekList.OrderBy(x => x.Line).GroupBy(x => x.Line).Select(x =>
                {
                    var arrDate = tempWeekList.Where(y => y.Line == x.Key).GroupBy(x => x.Date).Select(x => x.Key).ToList();
                    var arrValue = tempWeekList.Where(y => y.Line == x.Key).Select(z => z.arrValue).ToList();
                    return new
                    {
                        label = x.Key,
                        arrDate,
                        arrValue,

                    };

                }).ToList();
                return reportList;
            }
        }

        //整理資料依照日期補上沒有當日資料的產線
        private static void addLineforDate(List<BackMoreLineData> datalist)
        {
            var arrAllDateList = datalist.GroupBy(x => x.Date).Select(x => x.Key).ToList();
            var arrAllLineList = datalist.GroupBy(x => x.Line).Select(x => x.Key).ToList();
            foreach (var data1 in arrAllDateList)
            {
                foreach (var data2 in arrAllLineList)
                {
                    var check = datalist.FirstOrDefault(x => x.Date == data1 && x.Line == data2);
                    if (check == null)
                    {
                        BackMoreLineData backMoreLineData = new BackMoreLineData();
                        backMoreLineData.Date = data1;
                        backMoreLineData.Line = data2;
                        backMoreLineData.Value = "0";
                        datalist.Add(backMoreLineData);
                    }
                }
            }
        }

        public dynamic getMoreLinePerformanceData(FrontMoreLineData frontMoreLineData)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                _sql = $"SELECT Line,AVG(CAST({frontMoreLineData.selectType} as float)) AS Value ";
                _sql += " FROM [AIOT].[dbo].[Line_MachineData] ";
                _sql += " WHERE Item = @item AND Product = @product AND DATE BETWEEN @startDate AND @endDate AND Line IN @arrLine Group by Line Order by Value desc";
                var datalist = connection.Query<BackMoreLineData>(_sql, frontMoreLineData).ToList();
                return datalist;
            }
        }
        public dynamic getMoreLineERRData(FrontMoreLineData frontMoreLineData)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                _sql = "SELECT PL.LineName AS Line,SUM(CAST(LMS.Count as int)) AS Value ";
                _sql += $" FROM (SELECT * FROM [AIOT].[dbo].[Line_MachineERRData] WHERE Type = 'ERR' AND Date BETWEEN @startDate AND @endDate) AS LMS ";
                _sql += " LEFT JOIN [AIOT].[dbo].[Machine] AS M ON LMS.DeviceName = M.IODviceName ";
                _sql += " LEFT JOIN [AIOT].[dbo].[ProductProductionLines] AS PPL ON PPL.id = M.ProductProductionLinesID ";
                _sql += " LEFT JOIN [AIOT].[dbo].[ProductLine] AS PL ON PL.LineID = PPL.LineID ";
                _sql += " LEFT JOIN [AIOT].[dbo].[Product] AS P ON PPL.ProductID = P.ProductID ";
                _sql += " LEFT JOIN [AIOT].[dbo].[Item] AS I ON P.ItemID = I.ItemID ";
                //_sql += " LEFT JOIN [AIOT].[dbo].[Factory] AS F ON F.IODviceName = LMS.DeviceName ";
                _sql += " WHERE I.ItemName = @item AND P.ProductName = @product AND PL.LineName IN @arrLine GROUP BY PL.LineName Order by Value desc ";
                var datalist = connection.Query<BackMoreLineData>(_sql, frontMoreLineData).ToList();
                return datalist;
            }
        }

        public dynamic getStopTimeTable(string startTime, string endTime, string item, string product, string line, string device, string type)
        {
            _sql = "SELECT LMS.Date,LMS.StartTime,LMS.EndTime,LMS.SumTime,M.DeviceName ";
            switch (type.ToUpper())
            {
                case "UP":
                    _sql += $" FROM (SELECT * FROM [AIOT].[dbo].[Line_Machine_StopTenUp] WHERE Date BETWEEN '{startTime}' AND '{endTime}') AS LMS";
                    break;
                case "DOWN":
                    _sql += $" FROM (SELECT * FROM [AIOT].[dbo].[Line_Machine_StopTenDown] WHERE Date BETWEEN '{startTime}' AND '{endTime}') AS LMS";
                    break;
            }
            _sql += " LEFT JOIN [AIOT].[dbo].[Machine] AS M ON M.IODviceName = LMS.DeviceName ";
            _sql += " LEFT JOIN [AIOT].[dbo].[ProductProductionLines] AS PPL ON PPL.id = M.ProductProductionLinesID ";
            _sql += " LEFT JOIN [AIOT].[dbo].[ProductLine] AS PL ON PL.LineID = PPL.LineID ";
            _sql += " LEFT JOIN [AIOT].[dbo].[Product] AS P ON PPL.ProductID = P.ProductID ";
            _sql += " LEFT JOIN [AIOT].[dbo].[Item] AS I ON P.ItemID = I.ItemID ";
            _sql += $" WHERE I.ItemName ='{item}' AND P.ProductName = '{product}' AND PL.LineName = '{line}' ";
            _sql += device.ToUpper() == "ALL" ? "" : $" AND M.DeviceName = '{device}' ";
            _sql += " ORDER BY LMS.Date, LMS.SumTime DESC ";
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                var datalist = connection.Query<StopTimeTable>(_sql).ToList();
                return datalist;

            }
        }
        public dynamic getStopTimeTableERRCode(string strrtime, string endtime, string device)
        {
            var strtemp = Convert.ToDateTime(strrtime).AddSeconds(-10).ToString("yyyy-MM-dd HH:mm:ss.fff");
            _sql = "SELECT (select DeviceName FROM [AIOT].[dbo].[Machine] where IODviceName = MD.DeviceName) as DeviceName,MD.TIME, MD.Description ";
            _sql += " FROM [AIOT].[dbo].[Machine_Data] as MD ";
            _sql += $" where MD.DeviceName = (select IODviceName FROM [AIOT].[dbo].[Machine] where DeviceName = '{device}')";
            _sql += $" and MD.TIME between '{strtemp}' and '{endtime}'";
            _sql += " and (MD.NAME like '%PAT%' or MD.NAME like '%ERR%')";
            _sql += " and MD.VALUE = '1'";
            _sql += " order by TIME desc";
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                var datalist = connection.Query<StopTimeTableERRCode>(_sql).ToList();
                return datalist;

            }
        }
        public dynamic getErrData(string startTime, string endTime, string item, string product, string line, string type, string? device, string? avg)
        {
            //2024/03/19增加取出該產線有幾台機器
            _sql = type.ToUpper() == "ALL" ? $"SELECT LM.Date,Sum(Cast(LM.Count AS int)) AS 'Count',Min(LMD.ACT) AS'AllTime' " : $"SELECT LM.Date,LM.Type,Sum(Cast(LM.Count AS int)) AS 'Count',Min(LMD.ETC) AS AllTime ";
            _sql += " ,(select Count(M.IODviceName) FROM [AIOT].[dbo].[Machine] AS M ";
            _sql += " LEFT JOIN [AIOT].[dbo].[ProductProductionLines] AS PPL ON PPL.id = M.ProductProductionLinesID ";
            _sql += " LEFT JOIN [AIOT].[dbo].[ProductLine] AS PL ON PL.LineID = PPL.LineID ";
            _sql += " LEFT JOIN [AIOT].[dbo].[Product] AS P ON PPL.ProductID = P.ProductID ";
            _sql += " LEFT JOIN [AIOT].[dbo].[Item] AS I ON P.ItemID = I.ItemID ";
            _sql += $" WHERE I.ItemName ='{item}' AND P.ProductName = '{product}' AND PL.LineName = '{line}') as deviceCount ";
            _sql += $" FROM (SELECT * FROM [AIOT].[dbo].[Line_MachineERRData] WHERE Date Between '{startTime}' AND '{endTime}' ";
            //判斷是否有選擇ERR或者PAT，如果沒有就回傳全部臨停
            switch (type.ToUpper())
            {
                case "ERR":
                    _sql += $" AND TYPE ='{type}') AS LM";
                    break;
                case "PAT":
                    _sql += $" AND TYPE ='{type}') AS LM";
                    break;
                default:
                    _sql += ") AS LM";
                    break;
            }
            _sql += " LEFT JOIN  [AIOT].[dbo].[Machine] AS M ON LM.DeviceName = M.IODviceName ";
            _sql += " LEFT JOIN [AIOT].[dbo].[ProductProductionLines] AS PPL ON PPL.id = M.ProductProductionLinesID ";
            _sql += " LEFT JOIN [AIOT].[dbo].[ProductLine] AS PL ON PL.LineID = PPL.LineID ";
            _sql += " LEFT JOIN [AIOT].[dbo].[Product] AS P ON PPL.ProductID = P.ProductID ";
            _sql += " LEFT JOIN [AIOT].[dbo].[Item] AS I ON P.ItemID = I.ItemID ";
            _sql += " LEFT JOIN [AIOT].[dbo].[Line_MachineData] AS LMD ON LM.Date = LMD.Date and PL.LineName = LMD.Line ";
            _sql += $" WHERE I.ItemName ='{item}' AND P.ProductName = '{product}' AND PL.LineName = '{line}' ";

            //判斷是否有選擇device如果沒有就回傳整條線的臨停
            _sql += device.ToUpper() == "ALL" ? "" : $" AND M.DeviceName = '{device}'";
            //判斷是否有選擇ERR或者PAT，如果沒有就回傳全部臨停
            _sql += type.ToUpper() == "ALL" ? " GROUP BY LM.Date" : " GROUP BY LM.Date,LM.Type";
            _sql += " ORDER BY LM.Date";
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                var datalist = type.ToUpper() == "ALL" ? connection.Query<AVGErrData>(_sql, new { startTime, endTime, line }).ToList() : connection.Query<AVGErrData>(_sql, new { startTime, endTime, line, type }).ToList();
                if (datalist.Count == 0)
                {
                    return datalist;
                }
                //如果avg不是Null代表是平均臨停
                if (!string.IsNullOrEmpty(avg))
                {
                    foreach (var itemdata in datalist)
                    {
                        if (device.ToUpper() == "ALL")
                        {
                            //選擇產線機台全選需要除機台數量
                            itemdata.AVGCount = Math.Round((Convert.ToDouble(itemdata.Count) / (Convert.ToDouble(itemdata.AllTime)) / Convert.ToInt16(itemdata.deviceCount)), 2).ToString();
                        }
                        else
                        {
                            itemdata.AVGCount = Math.Round(Convert.ToDouble(itemdata.Count) / (Convert.ToDouble(itemdata.AllTime)), 2).ToString();
                        }

                    }
                }
                return datalist;
            }
        }

        public dynamic getOneLineERROrder(string startTime, string endTime, string item, string product, string line, string type, string reporttype, string device)
        {
            List<OneLineERROrder> datalist = new List<OneLineERROrder>();
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                switch (reporttype)
                {
                    case "date":
                        _sql = @" WITH LM_CTE AS (SELECT M.IODviceName AS DeviceName, PL.[LineName] AS ProductLine, LM.Date, LM.Time, LM.Type, LM.Name, LM.Count ";
                        _sql += $" FROM (SELECT * FROM [AIOT].[dbo].[Line_MachineERRData] WHERE Date BETWEEN '{startTime}' AND '{endTime}' AND Type = '{type}') AS LM";
                        _sql += " LEFT JOIN [AIOT].[dbo].[Machine] AS M ON M.IODviceName = LM.DeviceName";
                        _sql += " LEFT JOIN [AIOT].[dbo].[ProductProductionLines] AS PPL ON PPL.id = M.ProductProductionLinesID";
                        _sql += " LEFT JOIN [AIOT].[dbo].[ProductLine] AS PL ON PL.LineID = PPL.LineID";
                        _sql += " LEFT JOIN [AIOT].[dbo].[Product] AS P ON PPL.ProductID = P.ProductID";
                        _sql += " LEFT JOIN [AIOT].[dbo].[Item] AS I ON P.ItemID = I.ItemID";
                        _sql += $"  WHERE I.ItemName = '{item}' AND P.ProductName = '{product}' AND PL.LineName = '{line}' ";
                        _sql += device == "All" ? " )" : $" AND M.DeviceName = '{device}')";
                        _sql += " SELECT DeviceName,ProductLine,REPLACE(REPLACE(REPLACE(REPLACE(Time, CHAR(13) + CHAR(10), CHAR(13)), ' ',' '), CHAR(10)+CHAR(10), CHAR(10)),CHAR(10), '\n') AS Time,Date,Type,Name,Count";
                        _sql += " FROM LM_CTE";
                        _sql += " WHERE Count <> 0";
                        _sql += " ORDER BY Date DESC, CAST(Count AS INT) DESC ";
                        datalist.AddRange(connection.Query<OneLineERROrder>(_sql).ToList());

                        break;
                    case "week":
                        var listWeek = new List<Date>();
                        var startWeekYear = Convert.ToInt32(startTime.Split('-')[0]);
                        var endWeekYear = Convert.ToInt32(endTime.Split('-')[0]);
                        var countWeekYear = endWeekYear - startWeekYear;
                        var startWeek = Convert.ToInt32(startTime.Split("-")[1].Split("W")[1]);
                        //如果年份不一樣就乘上差異加到endWeek
                        var endWeek = countWeekYear > 0 ? Convert.ToInt32(endTime.Split("-")[1].Split("W")[1]) + (countWeekYear * 52) : Convert.ToInt32(endTime.Split("-")[1].Split("W")[1]);
                        var countWeek = endWeek - startWeek;
                        for (int i = 0; i <= countWeek; i++)
                        {

                            getDateforWeek(startWeekYear, startWeek, listWeek);
                            startWeek += 1;
                            if (startWeekYear < endWeekYear && startWeek > 52)
                            {
                                startWeekYear += 1;
                                startWeek = 1;
                            }
                        }
                        getOneLineERROrderWeekAndMonthReport(item, product, line, type, device, datalist, connection, listWeek);
                        break;
                    case "month":
                        var listMonth = new List<Date>();
                        var startMonthYear = Convert.ToInt32(startTime.Split('-')[0]);
                        var endtMonthYear = Convert.ToInt32(endTime.Split('-')[0]);
                        var countMonthYear = endtMonthYear - startMonthYear;
                        var startMonth = Convert.ToInt32(startTime.Split("-")[1]);
                        //如果年份不一樣就乘上差異加到endWeek
                        var endMonth = countMonthYear > 0 ? Convert.ToInt32(endTime.Split("-")[1]) + (countMonthYear * 12) : Convert.ToInt32(endTime.Split("-")[1]);
                        var countMonth = endMonth - startMonth;
                        for (int i = 0; i <= countMonth; i++)
                        {
                            getDatefoMonth(startMonthYear, startMonth, listMonth);
                            startMonth += 1;
                            if (startMonthYear < endtMonthYear && startMonth > 12)
                            {
                                startMonthYear += 1;
                                startMonth = 1;
                            }
                        }
                        getOneLineERROrderWeekAndMonthReport(item, product, line, type, device, datalist, connection, listMonth);
                        break;
                }
                if (datalist.Count == 0)
                {
                    return datalist;
                }
                //取出寄存器、錯誤訊息
                foreach (var itemdata in datalist)
                {
                    var temp = itemdata.Name.Split('_');
                    itemdata.Deposit = temp[0];
                    itemdata.ERRName = temp.Length > 2 ? temp[2] : temp[1].Split(' ').Length > 1 ? temp[1].Split(' ')[1] : temp[1].Split(' ')[0];
                }
                //2024-06-20 平均值
                //var test = datalist.GroupBy(x => x.Date).Select(y => new
                //{

                //    y.Key,
                //    SUM = y.Sum(x => Convert.ToInt32(x.Count)),

                //}).ToList();
                if (reporttype != "date")
                {
                    var tempdatalist = datalist.GroupBy(x => new { x.DeviceName, x.Date, x.Deposit, x.ERRName }).Select(y =>
                    {
                        var Time = string.Join("\n", y.Select(x => x.Time.Trim())).Trim();
                        //var Time = string.Join("\n", y.Select(x => Regex.Replace(x.Time, @"\s+  0", "\n0"))).Trim();
                        return new
                        {
                            y.Key.DeviceName,
                            y.Key.Date,
                            y.Key.Deposit,
                            y.Key.ERRName,
                            //Environment.NewLine 換行
                            //Time = string.Join("\n", y.Select(x => Regex.Replace(x.Time, @"\s+", " "))).Trim(),
                            Time,
                            Count = y.Sum(z => Convert.ToInt32(z.Count))
                        };
                    }).OrderByDescending(x => x.Date).ThenByDescending(x => x.Count).ToList();

                    return tempdatalist;
                }
                return datalist;


            }
        }

        public dynamic getDeviceName(string item, string product, string line)
        {
            _sql = "SELECT M.DeviceName FROM [AIOT].[dbo].[Machine] AS M ";
            _sql += " LEFT JOIN [AIOT].[dbo].[ProductProductionLines] AS PPL ON PPL.id = M.ProductProductionLinesID ";
            _sql += " LEFT JOIN [AIOT].[dbo].[ProductLine] AS PL ON PL.LineID = PPL.LineID ";
            _sql += " LEFT JOIN [AIOT].[dbo].[Product] AS P ON PPL.ProductID = P.ProductID ";
            _sql += " LEFT JOIN [AIOT].[dbo].[Item] AS I ON P.ItemID = I.ItemID ";
            _sql += $" WHERE I.ItemName = '{item}' AND P.ProductName = '{product}' AND PL.LineName = '{line}' ORDER BY  M.DeviceName";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                var datalist = connection.Query<string>(_sql).ToList();
                return datalist;
            }

        }
        public dynamic getAllLine(string product)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                _sql = $"SELECT PL.LineName FROM [AIOT].[dbo].[ProductProductionLines] AS PP LEFT JOIN [AIOT].[dbo].[ProductLine] AS PL ON  PP.LineID = PL.LineID LEFT JOIN [AIOT].[dbo].[Product] AS P ON P.ProductID = PP.ProductID WHERE P.ProductName = '{product}' ORDER BY LineName";
                var datalist = connection.Query<string>(_sql).ToList();
                return datalist;
            }

        }
        public dynamic getAllProduct(string item)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {

                _sql = $"SELECT P.ProductName FROM [AIOT].[dbo].[Product] AS P LEFT JOIN [AIOT].[dbo].[Item] AS I ON I.ItemID = P.ItemID WHERE I.ItemName = '{item}' ORDER BY ProductName";
                var datalist = connection.Query<string>(_sql).ToList();
                return datalist;
            }

        }
        public dynamic getAllItem()
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                _sql = "SELECT [ItemName] FROM[AIOT].[dbo].[Item] ORDER BY ItemName";
                var datalist = connection.Query<string>(_sql).ToList();
                return datalist;
            }

        }
        //週報使用
        public void getDateforWeek(int year, int week, List<Date> list)
        {
            //取出今年第一天
            var fristDate = new DateOnly(year, 1, 1);
            //今年第一天到第一周結束的日期還差距幾天
            var dateoffset = fristDate.DayOfWeek - DayOfWeek.Saturday;
            //今年第一天到第一周開始的日期還差距幾天
            var dateonset = fristDate.DayOfWeek - DayOfWeek.Sunday;
            var fristWeekStartDate = fristDate.AddDays(-(dateonset));
            var fristWeekEndDate = fristDate.AddDays(-(dateoffset));
            //要搜尋的周
            var targetEndDate = fristWeekEndDate.AddDays((week - 1) * 7).ToString("yyyy-MM-dd");
            var targetStartDate = fristWeekEndDate.AddDays((week - 1) * 7).AddDays(-6).ToString("yyyy-MM-dd");
            var data = new Date()
            {
                year = year.ToString(),
                week = week.ToString(),
                startDate = targetStartDate,
                endDate = targetEndDate
            };
            list.Add(data);
        }
        //月報
        public void getDatefoMonth(int year, int month, List<Date> list)
        {
            //取得當月的第一天
            var targetStartDate = new DateTime(year, month, 1);
            var nextMonth = targetStartDate.AddMonths(1).Month;
            var nextYear = targetStartDate.AddMonths(1).Year;
            var targetEndDate = new DateTime(nextYear, nextMonth, 1).AddDays(-1).ToString("yyyy-MM-dd");

            var data = new Date()
            {
                year = year.ToString(),
                month = month.ToString(),
                startDate = targetStartDate.ToString("yyyy-MM-dd"),
                endDate = targetEndDate
            };
            list.Add(data);
        }
        public dynamic getSCMData()
        {
            _sql = "SELECT * FROM [AIOT].[dbo].[Standard_Production_Efficiency_Benchmark]";
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                var datalist = connection.Query<SCM>(_sql).ToList();
                return datalist;
            }

        }
        public dynamic createORUpdateSCM(SCM scm)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                var check = false;
                switch (scm.Type)
                {
                    case "createProduct":
                        _sql = "SELECT TOP(1) * FROM [AIOT].[dbo].[Standard_Production_Efficiency_Benchmark] where Part_No = @Part_No AND Product_Name = @Product_Name AND PCS = @PCS  AND Model = @Model";
                        var createProductdatalist = connection.Query(_sql, scm).ToList();
                        if (createProductdatalist.Count == 0)
                        {
                            _sql = "INSERT INTO [AIOT].[dbo].[Standard_Production_Efficiency_Benchmark]VALUES(@Part_No,@Product_Name,@PCS,@Model)";
                            var count = connection.Execute(_sql, scm);
                            check = count > 0 ? true : false;
                        }
                        break;
                    case "createPCS":
                        _sql = "SELECT TOP(1) * FROM [AIOT].[dbo].[Standard_Production_Efficiency_Benchmark] where Part_No = @Part_No AND Product_Name = @Product_Name AND PCS = @PCS  AND Model = @Model";
                        var createPCSdatalist = connection.Query(_sql, scm).ToList();
                        if (createPCSdatalist.Count == 0)
                        {
                            _sql = "INSERT INTO [AIOT].[dbo].[Standard_Production_Efficiency_Benchmark]VALUES(@Part_No,@Product_Name,@PCS,@Model)";
                            var count = connection.Execute(_sql, scm);
                            check = count > 0 ? true : false;
                        }
                        break;
                    case "update":
                        _sql = "SELECT TOP(1) * FROM [AIOT].[dbo].[Standard_Production_Efficiency_Benchmark] where Part_No = @Part_No AND Product_Name = @Product_Name AND Model = @Model";
                        var updatedatalist = connection.Query(_sql, scm).ToList();
                        if (updatedatalist.Count > 0)
                        {
                            _sql = "UPDATE [AIOT].[dbo].[Standard_Production_Efficiency_Benchmark] SET PCS = @PCS  where Part_No = @Part_No AND Product_Name = @Product_Name AND Model = @Model";
                            var count = connection.Execute(_sql, scm);
                            check = count > 0 ? true : false;
                        }
                        break;
                }
                return check;

            }
        }
        public dynamic getKanBanProduct(string item)
        {
            _sql = $"SELECT P.ProductName FROM [AIOT].[dbo].[Product] AS P LEFT JOIN [AIOT].[dbo].[Item] AS I ON I.ItemID = P.ItemID WHERE I.ItemName = '{item}'";
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                var datalist = connection.Query<string>(_sql).ToList();
                return datalist;
            }
        }
        public dynamic getKanBanData(string item)
        {
            string date = DateTime.Now.ToString("yyyy-MM-dd");
            //date = "2024-04-29";
            _sql = $"select * ";
            _sql += $" from (SELECT PL.LineName,P.ProductName ";
            _sql += " FROM [AIOT].[dbo].[Item] as I ";
            _sql += " Join [AIOT].[dbo].[Product] as P on P.ItemID = I.ItemID ";
            _sql += " Join [AIOT].[dbo].[ProductProductionLines] as PPL on PPL.ProductID = P.ProductID ";
            _sql += $" Join [AIOT].[dbo].[ProductLine] as PL on PL.LineID = PPL.[LineID] where I.ItemName = '{item}') as AA ";
            _sql += $" join [AIOT].[dbo].[KanBan_Line_MachineData] as LM on LM.Line = AA.LineName AND LM.Date = '{date}' AND LM.Product = aa.ProductName  AND State <> '完成' order by AA.LineName";
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                var datalist = connection.Query<KanBanData>(_sql).ToList();
                return datalist;
            }
        }
        public dynamic saveReMark(requestSaveReMark reMark)
        {
            _sql = "UPDATE [AIOT].[dbo].[Line_MachineData] SET ReMark = @value WHERE Item = @item AND Product = @product AND Line = @line AND Date = @date";
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                var check = connection.Execute(_sql, reMark);
                return check > 0 ? true : false;
            }
        }

        //異常碼
        //_sql = @" WITH LM_CTE AS (SELECT M.IODviceName AS DeviceName, PL.[LineName] AS ProductLine, LM.Date, LM.Time, LM.Type, LM.Name, LM.Count, ROW_NUMBER() OVER (PARTITION BY LM.Date ORDER BY CAST(LM.Count AS INT) DESC) AS RowNum ";
        //        _sql += $" FROM (SELECT * FROM [AIOT].[dbo].[Line_MachineERRData] WHERE Date BETWEEN '{startTime}' AND '{endTime}' AND Type = '{type}') AS LM";
        //        _sql += " LEFT JOIN [AIOT].[dbo].[Machine] AS M ON M.IODviceName = LM.DeviceName";
        //        _sql += " LEFT JOIN [AIOT].[dbo].[ProductProductionLines] AS PPL ON PPL.id = M.ProductProductionLinesID";
        //        _sql += " LEFT JOIN [AIOT].[dbo].[ProductLine] AS PL ON PL.LineID = PPL.LineID";
        //        _sql += " LEFT JOIN [AIOT].[dbo].[Product] AS P ON PPL.ProductID = P.ProductID";
        //        _sql += " LEFT JOIN [AIOT].[dbo].[Item] AS I ON P.ItemID = I.ItemID";
        //        _sql += $"  WHERE I.ItemName = '{item}' AND P.ProductName = '{product}' AND PL.LineName = '{line}' ";
        //        _sql += device == "All" ? " )" : $" AND M.DeviceName = '{device}')";
        //        _sql += " SELECT DeviceName,ProductLine,Time,Date,Type,Name,Count";
        //        _sql += " FROM LM_CTE";
        //        //抓取前10筆資料
        //        _sql += " WHERE RowNum <= 10 AND Count <> 0";
        //        _sql += " ORDER BY Date DESC, CAST(Count AS INT) DESC ";

    }

}
