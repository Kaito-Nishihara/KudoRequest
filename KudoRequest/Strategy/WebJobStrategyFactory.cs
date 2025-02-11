namespace KudoRequest.Strategy
{
    public static class WebJobStrategyFactory
    {
        public static IWebJobStrategy GetStrategy(string jobType, List<WebJob> webJobs)
        {
            // WebJob のリストから一致するものを検索
            var selectedJob = webJobs.FirstOrDefault(j => j.Name.Equals(jobType, StringComparison.OrdinalIgnoreCase));

            if (selectedJob == null)
            {
                throw new ArgumentException($"無効な WebJobType: {jobType}");
            }

            // 一致する WebJob に対応する Strategy を返す
            return selectedJob.Name switch
            {
                "Poc" => new PocJobStrategy(),
                _ => throw new ArgumentException($"未定義の WebJobType: {jobType}"),
            };
        }
    }
}
