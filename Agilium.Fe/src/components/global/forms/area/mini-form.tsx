export interface MiniFormProps {
  title: string;
  children?: React.ReactNode;
  onSubmit: (e: React.SubmitEvent<HTMLFormElement>) => Promise<void>;
}

export function MiniForm(props: MiniFormProps) {
  return (<div className="flex bg-gray-50">
    <div className="w-full max-w-md bg-white rounded-lg shadow-lg p-8">
      <h1 className="text-2xl font-semibold text-gray-800 mb-6">{props.title}</h1>

      <form onSubmit={props.onSubmit} className="space-y-5">
        {props.children}
      </form>
    </div>
  </div>);
}