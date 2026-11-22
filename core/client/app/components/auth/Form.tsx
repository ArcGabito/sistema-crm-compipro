"use client";

import Image from "next/image";
import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { sileo } from "sileo";
import { LoaderCircle } from "lucide-react";
import { useAuth } from "../../hooks/useAuth";
import { ServidorNoDisponible } from "../../lib/auth";
import { despertarServidor, mientrasDespierta } from "../../lib/despertar";

export default function Form({ demo }: { demo?: { email: string; password: string } }) {
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const router = useRouter();
  const { login } = useAuth();

  // Despierta el servidor mientras se escriben las credenciales (escala a cero en Azure).
  useEffect(() => {
    void despertarServidor();
  }, []);

  const entrar = async (correo: string, clave: string) => {
    setIsLoading(true);
    try {
      const session = await mientrasDespierta(
        () => login(correo, clave),
        (error) => error instanceof ServidorNoDisponible,
      );
      sileo.success({ title: `Bienvenido, ${session.name || session.email}` });
      router.push("/dashboard");
    } catch (error) {
      if (error instanceof ServidorNoDisponible) {
        sileo.warning({
          title: "Servidor no disponible",
          description: "La demo no responde en este momento. Vuelve a intentarlo en unos minutos.",
        });
      } else {
        sileo.error({
          title: "No se pudo iniciar sesión",
          description:
            error instanceof Error ? error.message : "Inténtalo de nuevo.",
        });
      }
      setIsLoading(false);
    }
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    void entrar(email, password);
  };

  // Un clic: completa los campos con la cuenta demo y entra.
  const entrarDemo = () => {
    if (!demo) return;
    setEmail(demo.email);
    setPassword(demo.password);
    void entrar(demo.email, demo.password);
  };

  return (
    <form
      className="px-10 py-8 flex flex-col gap-6 min-w-85 max-w-100 [@media(max-height:620px)]:gap-3 [@media(max-height:620px)]:py-4"
      autoComplete="off"
      onSubmit={handleSubmit}
    >
      <div className="flex justify-center mb-2">
        <Image
          src={"/images/Compipro_Logo.webp"}
          alt="Logo Compipro"
          width={412}
          height={117}
          priority
          className="h-auto w-40 [@media(max-height:620px)]:w-28"
        />
      </div>
      <h1 className="text-3xl font-semibold tracking-tight text-center text-ink [@media(max-height:620px)]:text-2xl">
        ¡Bienvenido!
      </h1>
      <span className="text-center text-neutral-500 text-sm [@media(max-height:620px)]:hidden">
        Ingrese sus credenciales para acceder al sistema CRM de Compipro S.A.C.
      </span>
      {demo && (
        <p className="rounded-2xl bg-accent/15 px-4 py-3 text-sm text-neutral-700 ring-1 ring-accent/30 [@media(max-height:620px)]:py-2">
          <span className="font-semibold text-ink">Demo:</span> usa el botón «Entrar a la demo» o las credenciales que aparecen en los campos.
        </p>
      )}
      <div className="flex flex-col gap-1">
        <label htmlFor="email" className="text-neutral-700 text-sm font-medium pl-1">
          Correo Electrónico
        </label>
        <input
          id="email"
          type="email"
          placeholder={demo?.email ?? "usuario@compipro.net"}
          className="rounded-full bg-white px-5 py-3 [@media(max-height:620px)]:py-2 text-sm text-ink shadow-sm ring-1 ring-neutral-200 placeholder:text-neutral-400 focus:outline-none focus:ring-2 focus:ring-accent"
          autoComplete="email"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
        />
      </div>
      <div className="flex flex-col gap-1">
        <label htmlFor="password" className="text-neutral-700 text-sm font-medium pl-1">
          Contraseña
        </label>
        <input
          id="password"
          type="password"
          placeholder={demo?.password ?? "••••••••"}
          className="rounded-full bg-white px-5 py-3 [@media(max-height:620px)]:py-2 text-sm text-ink shadow-sm ring-1 ring-neutral-200 placeholder:text-neutral-400 focus:outline-none focus:ring-2 focus:ring-accent"
          autoComplete="current-password"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
        />
      </div>
      <button
        type="submit"
        disabled={isLoading}
        className="bg-ink hover:bg-neutral-800 disabled:opacity-60 text-white font-semibold rounded-full py-3 [@media(max-height:620px)]:py-2 transition-colors mt-2 shadow-md flex text-center items-center justify-center"
      >
        {isLoading && <LoaderCircle className="mr-2 size-4 animate-spin" />}
        Ingresar
      </button>
      {demo && (
        <button
          type="button"
          onClick={entrarDemo}
          disabled={isLoading}
          className="-mt-3 rounded-full bg-white py-3 text-sm font-semibold text-ink shadow-sm ring-1 ring-neutral-200 transition-colors hover:bg-neutral-50 disabled:opacity-60 [@media(max-height:620px)]:mt-0 [@media(max-height:620px)]:py-2"
        >
          Entrar a la demo
        </button>
      )}
    </form>
  );
}
