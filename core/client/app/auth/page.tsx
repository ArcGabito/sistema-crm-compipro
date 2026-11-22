import Form from "../components/auth/Form";
import Image from "next/image";

export default function HomePage() {
  // Credenciales de la demo pública (usuario con rol limitado sobre datos ficticios).
  // Sin configurar, no se muestran. La página es estática: cambiarlas requiere un nuevo build.
  const demo =
    process.env.DEMO_LOGIN_EMAIL && process.env.DEMO_LOGIN_PASSWORD
      ? { email: process.env.DEMO_LOGIN_EMAIL, password: process.env.DEMO_LOGIN_PASSWORD }
      : undefined;

  return (
    <section className="flex min-h-dvh items-center justify-center bg-frame p-4 [@media(max-height:620px)]:p-2">
      <div className="flex w-full max-w-4xl flex-col items-stretch gap-4 md:flex-row">
        <div className="flex flex-1 items-center justify-center rounded-[1.75rem] bg-panel shadow-sm ring-1 ring-white">
          <Form demo={demo} />
        </div>
        <div className="hidden flex-1 items-center justify-center rounded-[1.75rem] bg-panel p-6 shadow-sm ring-1 ring-white md:flex">
          <Image
            src={"/images/Mobile-login-rafiki.svg"}
            alt="Login Image"
            width={400}
            height={400}
            className="h-auto max-h-[60dvh] w-auto drop-shadow-xl"
          />
        </div>
      </div>
    </section>
  );
}
