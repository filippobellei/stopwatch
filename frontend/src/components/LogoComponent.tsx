//Author: Smeue
import { Link } from "react-router-dom";

export const LogoComponent = () => {
  return (
    <Link
      to="/"
      className="camera-logo flex items-center lg:pr-8 mr-8 hover:bg-[#0B1D32] hover:rounded-xl"
    >
      <img className="h-16" src="/imgs/CameraLogoT.svg" alt="logo" />
      <h1 className="lg:block hidden text-2xl font-bold text-white mr-8">
        STOPWATCH
      </h1>
    </Link>
  );
};
