import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  images: {
    remotePatterns: [{ protocol: "https", hostname: "**" }],
  },
  experimental: {
    // Receipt uploads run through a server action; the default 1 MB body limit
    // would reject legitimate files before our own 10 MB check runs. Allow headroom
    // above the 10 MB business limit for multipart encoding overhead.
    serverActions: {
      bodySizeLimit: "12mb",
    },
  },
};

export default nextConfig;
