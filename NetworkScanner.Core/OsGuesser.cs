namespace NetworkScanner
{
    // Ping 응답의 TTL로 OS 계열을 대략 추정한다. TTL은 홉마다 1씩 줄어들므로, 수신 TTL보다 크거나 같은
    // 가장 가까운 초기 TTL(64/128/255)로 역추정한다. 확실한 식별이 아니라 참고용이다.
    public static class OsGuesser
    {
        public static string FromTtl(int ttl)
        {
            if (ttl <= 0) return "";
            if (ttl <= 64) return "Linux/Unix/macOS";
            if (ttl <= 128) return "Windows";
            return "네트워크 장비";
        }
    }
}
