 找工之余，对代码进行整理开源（虚线部分表示未完成)
![image](http://122.114.170.153:8181/Public/images/NPublic.png)

一、NDapper(数据库操作)  
 基于Dapper的二次封装。对数据库的操作失败记录进行统一收集管理  
![image](http://122.114.170.153:8181/Public/images/NDapper.png)  
1、支持SqlServer，MySql，SqlLite等常用数据库，传入数据库连接字符串开箱即用，简化封装  
2、常规数据库操作CURD需要添加try catch,否则会导致系统崩溃  
3、即便添加try catch，整个系统的错误日志也分散在不同地方  
4、对试探性攻击，操作异常等难以实时监控  
5、NDapper内置异常捕获，对DB的操作不再需要再添加try catch，只需关注业务逻辑编码  
6、继承Dapper，方法函数完全等同Dapper，仅对异常进行捕获。没有性能损耗  
7、内置原生.NET ADO数据库操作，便于测试对比  


二、NLog（日志）  
轻量级，基于队列的实时日志系统  
 ![image](http://122.114.170.153:8181/Public/images/NLog.png)  
1、NLog先将产生的日志依次压入队列中  
2、队列中的日志可供监控使用，实时显示  
3、同时提供三种落地保存方式：文件，MQ和数据库  
4、采用发布-订阅模式。NLog初始化后前端可以订阅产生的日志，订阅到日志后自己编码处理  
与Log4不同，NLog主要优势是将所有日志按顺序收集到队列后再进行后续处理,以能够集中实时监控处理  


三、NDI（轻型IOC容器）  
轻量级，依赖注入容器，便于系统集成  
 ![image](http://122.114.170.153:8181/Public/images/NDI.png)  
除了项目内部类注册，也可以对其他项目编译的DLL直接注入  

四、NSocket（TCP通讯）（整理中...）  
 轻量级，高效，可靠的TCP通讯解决框架，方便自定义自己的通讯协议  
![image](http://122.114.170.153:8181/Public/images/NSocket.png)  
1、轻量级，高效，可靠的TCP通讯解决框架，方便自定义自己的通讯协议  
2、基于原始TCP封装，损耗最小，速度最快。稳定，可靠  
3、方便自定义，支持字符串，二进制等各种数据传输  
（该部分代码完善中）  

五、NPublic.UI(winform皮肤，插件)（整理中...）  
轻量级，扁平化皮肤。可轻松实现类似QQ播放器，360软件等界面。  
 ![image](http://122.114.170.153:8181/Public/images/f1.png)  
 ![image](http://122.114.170.153:8181/Public/images/f2.png)  
 ![image](http://122.114.170.153:8181/Public/images/f3.png)  

备用  
http://122.114.170.153:8181/Public/NPublic.html
