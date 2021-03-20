using System;
using System.Data;
using System.Linq;
using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider.Oracle;
using LinqToDB.Mapping;
using Moq;

var p = new Mock<IDbDataParameter>();

var ps = new Mock<IDataParameterCollection>();
ps.Setup(x => x.Count).Returns(0);
ps.Setup(x => x.GetEnumerator()).Returns(Array.Empty<object>().GetEnumerator());

var cmd = new Mock<IDbCommand>();
cmd.Setup(x => x.Parameters).Returns(ps.Object);
cmd.Setup(x => x.CreateParameter()).Returns(p.Object);
cmd.SetupProperty(x => x.CommandText);

var con = new Mock<IDbConnection>();
con.Setup(x => x.CreateCommand()).Returns(cmd.Object);

var db = new DataConnection(
	new OracleDataProvider("ODP"),
	con.Object);

var q = from s in db.GetTable<Src>()
		where s.Cond > 4
		select new { s.N, s.Name, Frob = s.N + s.Cond, Unused = s.Name + "_NOT_USED" };

var param = "some parameter";

q.MultiInsert()
	.Into(
		db.GetTable<Dest1>(),
		x => new Dest1 { Name = x.Name ?? param, A = x.N })
	.Into(
		db.GetTable<Dest1>(),
		x => new Dest1 { Name = param + x.Name, A = 42 })
	.Into(
		db.GetTable<Dest2>(),
		x => new Dest2 { X = x.N, Y = 1 - x.Frob })		
	.Insert();

Console.WriteLine("--- INSERT ALL (no condition)");
Console.WriteLine(cmd.Object.CommandText);

q.MultiInsert()
	.When(
		x => x.N > 10,
		db.GetTable<Dest1>(),
		x => new Dest1 { Name = x.Name, A = x.N }
	)
	.When(
		x => x.N > 100,
		db.GetTable<Dest1>(),
		x => new Dest1 { Name = "This is crazy!", A = x.Frob }
	)
	.Else(
		db.GetTable<Dest2>(),
		x => new Dest2 { X = 1, Y = x.Frob + 2 }
	)
	.InsertFirst();

Console.WriteLine("--- INSERT FIRST");
Console.WriteLine(cmd.Object.CommandText);

q.MultiInsert()
	.When(
		x => param.Length > 10,
		db.GetTable<Dest1>(),
		x => new Dest1 { Name = x.Name, A = x.N }
	)
	.When(
		x => x.N > 100,
		db.GetTable<Dest1>(),
		x => new Dest1 { Name = param, A = x.Frob }
	)
	.When(
		x => true,
		db.GetTable<Dest2>(),
		x => new Dest2 { X = 1, Y = x.Frob + 2 }
	)
	.InsertAll();

Console.WriteLine("--- INSERT ALL (conditions)");
Console.WriteLine(cmd.Object.CommandText);

class Src 
{
	public string Name { get; set; }
	public int N { get; set; }
	public int Cond { get; set; }
}

class Dest1
{
	[PrimaryKey, Identity]
	public int Id { get; set; }	
	public string Name { get; set; }
	public int A { get; set; }
}

class Dest2
{
	[PrimaryKey, Identity]
	public int Id { get; set; }
	public int X { get; set; }
	public int Y { get; set; }
}
