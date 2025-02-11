namespace KudoRequest.Strategy
{
    public static class WebJobStrategyFactory
    {
        public static IWebJobStrategy GetStrategy(string jobType)
        {
            // 大文字小文字を無視して Enum に変換
            if (Enum.TryParse<WebJobType>(jobType, true, out var parsedType))
            {
                return parsedType switch
                {
                    WebJobType.Poc => new PocJobStrategy(),
                    _ => throw new ArgumentException($"未定義の WebJobType: {jobType}"),
                };
            }

            throw new ArgumentException($"無効な WebJobType: {jobType}");
        }

    }

}
