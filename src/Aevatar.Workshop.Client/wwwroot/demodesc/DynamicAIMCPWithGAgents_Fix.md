# Dynamic AI Agent - GAgent工具参数传递修复

## 问题描述
在使用AI Agent调用MathGAgent时，Expression参数为空，导致数学计算失败。

## 根本原因
在`CallGAgentToolAsync`方法中，虽然创建了事件实例，但没有正确地将参数值设置到事件对象的属性中。

## 解决方案

### 1. 参数映射逻辑改进
修改了事件对象创建和参数设置逻辑：
- 使用反射将JSON参数映射到事件属性
- 支持简单值和复杂对象的参数传递
- 添加类型转换以确保参数类型匹配

### 2. Semantic Kernel函数参数改进
更新了GAgent工具注册逻辑：
- 从事件类型中提取属性作为函数参数
- 为每个参数生成正确的元数据
- 使用KernelArguments传递参数而不是字符串

### 3. 类型转换增强
添加了`ConvertJsonElementToPropertyType`方法：
- 处理所有基本类型（string、int、double、bool等）
- 支持可空类型
- 处理DateTime和Guid等特殊类型

## 测试用例

### 数学计算
```
用户: 计算 250 的 15% 是多少？
AI: 使用 MathGAgent 计算表达式 "250 * 0.15"
结果: 37.5
```

### 时间转换
```
用户: 现在东京几点？
AI: 使用 TimeConverterGAgent 转换时间
结果: [显示东京当前时间]
```

## 技术细节

### CallGAgentToolAsync改进
```csharp
// 创建事件实例
EventBase? @event = Activator.CreateInstance(eventType) as EventBase;

// 使用反射设置属性
var eventProperty = eventType.GetProperty(property.Name, 
    BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
    
if (eventProperty != null && eventProperty.CanWrite)
{
    var value = ConvertJsonElementToPropertyType(property.Value, eventProperty.PropertyType);
    eventProperty.SetValue(@event, value);
}
```

### 函数参数生成
```csharp
// 从事件属性生成Kernel参数
var eventProperties = eventType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
    .Where(p => p.CanWrite && p.Name != "CorrelationId" && p.Name != "PublisherGrainId")
    .ToList();

parameters: eventProperties.Select(p => new KernelParameterMetadata(p.Name)
{
    Description = $"Parameter {p.Name} of type {p.PropertyType.Name}",
    IsRequired = true,
    ParameterType = p.PropertyType
}).ToArray()
```

## 重要提示
- 确保事件类型的属性名称与AI模型期望的参数名称匹配
- 对于复杂参数，确保JSON序列化格式正确
- 监控日志以验证参数传递是否成功 